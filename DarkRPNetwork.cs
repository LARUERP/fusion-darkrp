using System;
using System.Reflection;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Senders;
using LabFusion.SDK.Modules;

namespace FusionDarkRP
{
    public sealed class DarkRPRequestData : INetSerializable
    {
        public byte TargetId;
        public byte Team;
        public byte Grade;

        public int? GetSize() => 3;

        public void Serialize(INetSerializer serializer)
        {
            serializer.SerializeValue(ref TargetId);
            serializer.SerializeValue(ref Team);
            serializer.SerializeValue(ref Grade);
        }
    }

    public sealed class DarkRPRequestHandler : ModuleMessageHandler
    {
        public override ExpectedReceiverType ExpectedReceiver =>
            ExpectedReceiverType.ServerOnly;

        protected override bool OnPreRelayMessage(ReceivedMessage received)
        {
            return received.Sender.HasValue &&
                   received.Bytes != null &&
                   received.Bytes.Length == 3;
        }

        protected override void OnHandleMessage(ReceivedMessage received)
        {
            if (!NetworkInfo.IsHost || !received.Sender.HasValue)
                return;

            try
            {
                DarkRPRequestData data =
                    received.ReadData<DarkRPRequestData>();

                PlayerID sender =
                    PlayerIDManager.GetPlayerID(received.Sender.Value);

                PlayerID target =
                    PlayerIDManager.GetPlayerID(data.TargetId);

                if (sender == null || target == null ||
                    !sender.IsValid || !target.IsValid)
                    return;

                if (!DarkRPNetwork.CanApply(
                    sender, target, data.Team, data.Grade))
                    return;

                string team = DarkRPNetwork.TeamName(data.Team);
                string grade = DarkRPNetwork.GradeName(data.Grade);

                if (team == null || grade == null)
                    return;

                PlayerSender.SendPlayerMetadataResponse(
                    target.SmallID,
                    DarkRPNetwork.TeamKey,
                    team);

                PlayerSender.SendPlayerMetadataResponse(
                    target.SmallID,
                    DarkRPNetwork.GradeKey,
                    grade);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error(
                    "[Fusion DarkRP] Erreur traitement demande : " + ex);
            }
        }
    }

    public static class DarkRPNetwork
    {
        public const string TeamKey = "FusionDarkRP.Team";
        public const string GradeKey = "FusionDarkRP.Grade";

        private static bool registered;

        public static void Register()
        {
            if (registered)
                return;

            ModuleMessageManager.LoadHandlers(
                typeof(DarkRPNetwork).Assembly);

            registered = true;
        }

        public static void Request(
            byte targetId,
            byte team,
            byte grade)
        {
            if (!NetworkInfo.HasServer)
                return;

            DarkRPRequestData data = new DarkRPRequestData
            {
                TargetId = targetId,
                Team = team,
                Grade = grade
            };

            MessageRelay.RelayModule<
                DarkRPRequestHandler,
                DarkRPRequestData>(
                data,
                CommonMessageRoutes.ReliableToServer);
        }

        public static bool CanApply(
            PlayerID sender,
            PlayerID target,
            byte team,
            byte grade)
        {
            if (sender == null || target == null)
                return false;

            if (!sender.IsValid || !target.IsValid)
                return false;

            string requestedTeam = TeamName(team);
            string requestedGrade = GradeName(grade);

            if (requestedTeam == null || requestedGrade == null)
                return false;

            if (!IsValidPair(team, grade))
                return false;

            // L'hôte peut gérer toutes les équipes et tous les grades.
            if (IsHostPlayer(sender))
                return true;

            string targetTeam = GetMetadata(target, TeamKey);
            string targetGrade = GetMetadata(target, GradeKey);

            // Un joueur ne peut gérer que son propre personnage
            // pour les métiers publics autorisés.
            if (sender.SmallID == target.SmallID)
            {
                return (team == 0 && grade == 0) ||
                       (team == 1 && grade == 1);
            }

            // Un chef ne peut jamais se gérer lui-même.
            if (sender.SmallID == target.SmallID)
                return false;

            string managerTeam = GetMetadata(sender, TeamKey);
            string managerGrade = GetMetadata(sender, GradeKey);

            if (string.IsNullOrEmpty(managerTeam) ||
                string.IsNullOrEmpty(managerGrade))
                return false;

            int managerRank = Rank(managerTeam, managerGrade);

            string[] hierarchy = Ranks(managerTeam);

            // Seul le grade le plus élevé de son équipe
            // peut recruter et gérer les membres.
            if (hierarchy.Length == 0 ||
                managerRank != hierarchy.Length - 1)
                return false;

            // Le chef ne peut gérer que sa propre équipe.
            if (managerTeam != requestedTeam &&
                !(team == 0 && grade == 0))
                return false;

            // Un chef ne peut jamais attribuer le grade de chef.
            if (IsTopRankName(requestedGrade))
                return false;

            // Un chef ne peut pas recruter un autre chef.
            if (IsTopRankName(targetGrade))
                return false;

            // Le renvoi en citoyen est réservé aux membres
            // de sa propre équipe et aux grades inférieurs.
            if (team == 0 && grade == 0)
            {
                if (targetTeam != managerTeam)
                    return false;

                int targetRank = Rank(targetTeam, targetGrade);

                return targetRank >= 0 &&
                       targetRank < managerRank;
            }

            // Le recrutement doit viser la propre équipe du chef.
            if (targetTeam == managerTeam)
            {
                int oldRank = Rank(targetTeam, targetGrade);

                // Interdit de modifier un membre de rang égal
                // ou supérieur au sien.
                if (oldRank < 0 || oldRank >= managerRank)
                    return false;
            }

            // Un joueur d'une autre équipe peut être recruté,
            // sauf s'il est lui-même chef de cette équipe.
            return Rank(requestedTeam, requestedGrade) >= 0 &&
                   Rank(requestedTeam, requestedGrade) < managerRank;
        }

        private static string GetMetadata(
            PlayerID player,
            string key)
        {
            try
            {
                return player.Metadata.Metadata.GetMetadata(key);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsHostPlayer(PlayerID player)
        {
            if (player == null)
                return false;

            try
            {
                PropertyInfo property =
                    player.GetType().GetProperty("IsHost");

                if (property != null &&
                    property.PropertyType == typeof(bool))
                {
                    return (bool)property.GetValue(player);
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool IsValidPair(byte team, byte grade)
        {
            string teamName = TeamName(team);
            string gradeName = GradeName(grade);

            return teamName != null &&
                   gradeName != null &&
                   Rank(teamName, gradeName) >= 0;
        }

        private static bool IsTopRankName(string grade)
        {
            return grade == "Maire" ||
                   grade == "ChefPolice" ||
                   grade == "MedecinChef" ||
                   grade == "ChefEboueurs" ||
                   grade == "DirecteurBanque" ||
                   grade == "GerantEpicerie" ||
                   grade == "Patron";
        }

        private static string[] Ranks(string team)
        {
            switch (team)
            {
                case "Mairie":
                    return new[]
                    {
                        "ConseillerMunicipal",
                        "SecretaireMairie",
                        "AdjointMaire",
                        "Maire"
                    };

                case "Police":
                    return new[]
                    {
                        "Policier",
                        "Sergent",
                        "LieutenantPolice",
                        "CommissaireAdjoint",
                        "ChefPolice"
                    };

                case "Medecins":
                    return new[]
                    {
                        "Infirmier",
                        "Chirurgien",
                        "MedecinAdjoint",
                        "MedecinChef"
                    };

                case "Eboueurs":
                    return new[]
                    {
                        "Eboueur",
                        "ResponsableCollecte",
                        "ChefEboueurs"
                    };

                case "Banque":
                    return new[]
                    {
                        "Guichetier",
                        "ConseillerBancaire",
                        "DirecteurAdjointBanque",
                        "DirecteurBanque"
                    };

                case "Epicerie":
                    return new[]
                    {
                        "Vendeur",
                        "ResponsableRayon",
                        "GerantAdjoint",
                        "GerantEpicerie"
                    };

                case "Marseillais":
                case "Parisiens":
                case "GensDuVoyage":
                    return new[]
                    {
                        "Membre",
                        "Associe",
                        "LieutenantGang",
                        "BrasDroit",
                        "Patron"
                    };

                default:
                    return new string[0];
            }
        }

        private static int Rank(string team, string grade)
        {
            if (team == "Citoyen")
                return grade == "Citoyen" ? 0 : -1;

            string[] ranks = Ranks(team);

            for (int i = 0; i < ranks.Length; i++)
            {
                if (ranks[i] == grade)
                    return i;
            }

            return -1;
        }

        public static string TeamName(byte value)
        {
            string[] names =
            {
                "Citoyen",
                "Eboueurs",
                "Banque",
                "Mairie",
                "Epicerie",
                "Police",
                "Medecins",
                "Marseillais",
                "Parisiens",
                "GensDuVoyage"
            };

            return value < names.Length ? names[value] : null;
        }

        public static string GradeName(byte value)
        {
            string[] names =
            {
                "Citoyen",
                "Eboueur",
                "ResponsableCollecte",
                "ChefEboueurs",
                "Guichetier",
                "ConseillerBancaire",
                "DirecteurAdjointBanque",
                "DirecteurBanque",
                "ConseillerMunicipal",
                "SecretaireMairie",
                "AdjointMaire",
                "Maire",
                "Vendeur",
                "ResponsableRayon",
                "GerantAdjoint",
                "GerantEpicerie",
                "Policier",
                "Sergent",
                "LieutenantPolice",
                "CommissaireAdjoint",
                "ChefPolice",
                "Infirmier",
                "Chirurgien",
                "MedecinAdjoint",
                "MedecinChef",
                "Membre",
                "Associe",
                "LieutenantGang",
                "BrasDroit",
                "Patron"
            };

            return value < names.Length ? names[value] : null;
        }
    }
}

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
            byte targetId, byte team, byte grade)
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
            string requestedTeam = TeamName(team);
            string requestedGrade = GradeName(grade);

            if (requestedTeam == null || requestedGrade == null)
                return false;

            // L'hote peut attribuer les grades, y compris les postes
            // de chef et de patron.
            if (IsHostPlayer(sender))
                return IsValidPair(team, grade);

            string targetTeam =
                target.Metadata.Metadata.GetMetadata(TeamKey);

            string targetGrade =
                target.Metadata.Metadata.GetMetadata(GradeKey);

            // Un joueur peut uniquement redevenir citoyen
            // ou rejoindre les eboueurs au grade de base.
            if (sender.SmallID == target.SmallID)
            {
                return (team == 0 && grade == 0) ||
                       (team == 1 && grade == 1);
            }

            string managerTeam =
                sender.Metadata.Metadata.GetMetadata(TeamKey);

            string managerGrade =
                sender.Metadata.Metadata.GetMetadata(GradeKey);

            if (string.IsNullOrEmpty(managerTeam) ||
                string.IsNullOrEmpty(managerGrade))
                return false;

            // Un responsable ne peut gerer que sa propre equipe.
            if (managerTeam != requestedTeam)
                return false;

            string[] hierarchy = Ranks(managerTeam);

            int managerRank =
                Rank(managerTeam, managerGrade);

            int requestedRank =
                Rank(requestedTeam, requestedGrade);

            // IMPORTANT :
            // Seul le dernier grade de la hierarchie peut gerer
            // les membres de l'equipe.
            if (hierarchy.Length == 0 ||
                managerRank != hierarchy.Length - 1)
                return false;

            // Un chef ou patron ne peut pas nommer un autre chef
            // ou patron. Cette action est reservee a l'hote.
            if (IsTopRankName(requestedGrade))
                return false;

            // Le responsable ne peut recruter que des citoyens,
            // des eboueurs ou des membres de sa propre equipe.
            if (!string.IsNullOrEmpty(targetTeam) &&
                targetTeam != "Citoyen" &&
                targetTeam != "Eboueurs" &&
                targetTeam != requestedTeam)
                return false;

            int oldRank = Rank(targetTeam, targetGrade);

            // Impossible de modifier un membre de rang egal
            // ou superieur au sien.
            if (targetTeam == requestedTeam &&
                oldRank >= managerRank)
                return false;

            // Renvoyer citoyen uniquement un membre de son equipe.
            // Impossible de renvoyer un chef ou un patron.
            if (team == 0 && grade == 0)
            {
                return targetTeam == requestedTeam &&
                       oldRank >= 0 &&
                       oldRank < managerRank &&
                       !IsTopRankName(targetGrade);
            }

            // Le responsable peut attribuer uniquement les grades
            // inferieurs au sien.
            return requestedRank >= 0 &&
                   requestedRank < managerRank;
        }

        private static bool IsHostPlayer(PlayerID player)
        {
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

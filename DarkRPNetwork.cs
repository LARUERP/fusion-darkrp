
using System;
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

        public int? GetSize()
        {
            return 3;
        }

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

        public static void Register()
        {
            ModuleMessageManager.LoadHandlers(
                typeof(DarkRPNetwork).Assembly);
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
            string requestedTeam = TeamName(team);
            string requestedGrade = GradeName(grade);

            if (requestedTeam == null || requestedGrade == null)
                return false;

            // L'hote peut attribuer tous les postes.
            if (sender.IsHost)
                return true;

            string currentTeam =
                sender.Metadata.Metadata.GetMetadata(TeamKey);

            string currentGrade =
                sender.Metadata.Metadata.GetMetadata(GradeKey);

            // Un joueur peut choisir uniquement un poste public
            // pour lui-même.
            if (sender.SmallID == target.SmallID)
                return IsPublicJob(team, grade);

            // Un responsable ne peut gérer que sa propre équipe.
            if (string.IsNullOrEmpty(currentTeam) ||
                string.IsNullOrEmpty(currentGrade) ||
                currentTeam != requestedTeam)
                return false;

            int managerRank = Rank(currentTeam, currentGrade);
            int targetRank = Rank(requestedTeam, requestedGrade);

            if (managerRank < 1 ||
                targetRank < 0 ||
                targetRank >= managerRank)
                return false;

            // Les chefs et patrons restent nommés par l'hôte.
            if (IsTopRank(grade))
                return false;

            return true;
        }

        private static bool IsPublicJob(byte team, byte grade)
        {
            string t = TeamName(team);
            string g = GradeName(grade);

            if (t == null || g == null)
                return false;

            if (t == "Citoyen" && g == "Citoyen")
                return true;

            if (t == "Eboueurs" && g == "Eboueur")
                return true;

            if (t == "Banque" && g == "Guichetier")
                return true;

            if (t == "Mairie" && g == "ConseillerMunicipal")
                return true;

            if (t == "Epicerie" && g == "Vendeur")
                return true;

            return false;
        }

        private static bool IsTopRank(byte grade)
        {
            string g = GradeName(grade);

            return g == "Maire" ||
                   g == "ChefPolice" ||
                   g == "MedecinChef" ||
                   g == "ChefEboueurs" ||
                   g == "DirecteurBanque" ||
                   g == "GerantEpicerie" ||
                   g == "Patron";
        }

        private static int Rank(string team, string grade)
        {
            string[] ranks;

            switch (team)
            {
                case "Mairie":
                    ranks = new[] {
                        "ConseillerMunicipal",
                        "SecretaireMairie",
                        "AdjointMaire",
                        "Maire"
                    };
                    break;

                case "Police":
                    ranks = new[] {
                        "Policier",
                        "Sergent",
                        "LieutenantPolice",
                        "CommissaireAdjoint",
                        "ChefPolice"
                    };
                    break;

                case "Medecins":
                    ranks = new[] {
                        "Infirmier",
                        "Chirurgien",
                        "MedecinAdjoint",
                        "MedecinChef"
                    };
                    break;

                case "Eboueurs":
                    ranks = new[] {
                        "Eboueur",
                        "ResponsableCollecte",
                        "ChefEboueurs"
                    };
                    break;

                case "Banque":
                    ranks = new[] {
                        "Guichetier",
                        "ConseillerBancaire",
                        "DirecteurAdjointBanque",
                        "DirecteurBanque"
                    };
                    break;

                case "Epicerie":
                    ranks = new[] {
                        "Vendeur",
                        "ResponsableRayon",
                        "GerantAdjoint",
                        "GerantEpicerie"
                    };
                    break;

                case "Marseillais":
                case "Parisiens":
                case "GensDuVoyage":
                    ranks = new[] {
                        "Membre",
                        "Associe",
                        "LieutenantGang",
                        "BrasDroit",
                        "Patron"
                    };
                    break;

                default:
                    return -1;
            }

            for (int i = 0; i < ranks.Length; i++)
            {
                if (ranks[i] == grade)
                    return i;
            }

            return -1;
        }

        public static string TeamName(byte value)
        {
            string[] values = {
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

            return value < values.Length ? values[value] : null;
        }

        public static string GradeName(byte value)
        {
            string[] values = {
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

            return value < values.Length ? values[value] : null;
        }
    }
}

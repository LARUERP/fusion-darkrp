
using System;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Senders;
using LabFusion.SDK.Modules;
using LabFusion.UI.Popups;

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

    public sealed class DarkRPNotificationData : INetSerializable
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

    public sealed class DarkRPNotificationHandler : ModuleMessageHandler
    {
        public override ExpectedReceiverType ExpectedReceiver =>
            ExpectedReceiverType.ClientsOnly;

        protected override bool OnPreRelayMessage(ReceivedMessage received)
        {
            return received.Bytes != null && received.Bytes.Length == 3;
        }

        protected override void OnHandleMessage(ReceivedMessage received)
        {
            try
            {
                DarkRPNotificationData data =
                    received.ReadData<DarkRPNotificationData>();

                PlayerID local = PlayerIDManager.LocalID;

                if (local == null || !local.IsValid ||
                    local.SmallID != data.TargetId)
                    return;

                string team = DarkRPNetwork.TeamName(data.Team);
                string grade = DarkRPNetwork.GradeName(data.Grade);

                if (team == null || grade == null)
                    return;

                Notifier.Send(new Notification
                {
                    Title = "Fusion DarkRP",
                    Message = "Ton métier : " + team +
                              " | Grade : " + grade,
                    Type = NotificationType.SUCCESS,
                    PopupLength = 4f,
                    SaveToMenu = true
                });
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error(
                    "[Fusion DarkRP] Erreur notification : " + ex);
            }
        }
    }

    public sealed class DarkRPRequestHandler : ModuleMessageHandler
    {
        public override ExpectedReceiverType ExpectedReceiver =>
            ExpectedReceiverType.ServerOnly;

        protected override bool OnPreRelayMessage(ReceivedMessage received)
        {
            // Sender n'est pas encore renseigne pendant cette etape.
            return received.Bytes != null && received.Bytes.Length == 3;
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
                {
                    MelonLoader.MelonLogger.Warning(
                        "[Fusion DarkRP] Attribution refusee : permissions insuffisantes.");
                    return;
                }

                string team = DarkRPNetwork.TeamName(data.Team);
                string grade = DarkRPNetwork.GradeName(data.Grade);

                if (team == null || grade == null)
                    return;

                // Mise a jour immediate des donnees connues de l'hote.
                target.Metadata.Metadata.ForceSetLocalMetadata(
                    DarkRPNetwork.TeamKey, team);

                target.Metadata.Metadata.ForceSetLocalMetadata(
                    DarkRPNetwork.GradeKey, grade);

                // Diffusion des deux metadonnees aux clients.
                PlayerSender.SendPlayerMetadataResponse(
                    target.SmallID,
                    DarkRPNetwork.TeamKey,
                    team);

                PlayerSender.SendPlayerMetadataResponse(
                    target.SmallID,
                    DarkRPNetwork.GradeKey,
                    grade);

                // Notification de l'hote si la cible est lui-meme.
                PlayerID local = PlayerIDManager.LocalID;

                if (local != null && local.IsValid &&
                    local.SmallID == target.SmallID)
                {
                    DarkRPNetwork.ShowNotification(team, grade);
                }
                else
                {
                    // Les clients filtrent la notification par TargetId.
                    MessageRelay.RelayModule<
                        DarkRPNotificationHandler,
                        DarkRPNotificationData>(
                        new DarkRPNotificationData
                        {
                            TargetId = target.SmallID,
                            Team = data.Team,
                            Grade = data.Grade
                        },
                        CommonMessageRoutes.ReliableToClients);
                }

                MelonLoader.MelonLogger.Msg(
                    "[Fusion DarkRP] Attribution appliquee : joueur ID " +
                    target.SmallID + " -> " + team + " / " + grade);
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

        public static void Request(byte targetId, byte team, byte grade)
        {
            if (!NetworkInfo.HasServer)
            {
                MelonLoader.MelonLogger.Warning(
                    "[Fusion DarkRP] Aucune connexion serveur.");
                return;
            }

            DarkRPRequestData data = new DarkRPRequestData
            {
                TargetId = targetId,
                Team = team,
                Grade = grade
            };

            MelonLoader.MelonLogger.Msg(
                "[Fusion DarkRP] Demande : cible " + targetId +
                ", equipe " + team + ", grade " + grade);

            MessageRelay.RelayModule<
                DarkRPRequestHandler,
                DarkRPRequestData>(
                data,
                CommonMessageRoutes.ReliableToServer);
        }

        public static void ShowNotification(string team, string grade)
        {
            try
            {
                Notifier.Send(new Notification
                {
                    Title = "Fusion DarkRP",
                    Message = "Ton métier : " + team +
                              " | Grade : " + grade,
                    Type = NotificationType.SUCCESS,
                    PopupLength = 4f,
                    SaveToMenu = true
                });
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning(
                    "[Fusion DarkRP] Notification impossible : " + ex.Message);
            }
        }

        public static bool CanApply(
            PlayerID sender,
            PlayerID target,
            byte team,
            byte grade)
        {
            if (sender == null || target == null ||
                !sender.IsValid || !target.IsValid)
                return false;

            string requestedTeam = TeamName(team);
            string requestedGrade = GradeName(grade);

            if (requestedTeam == null || requestedGrade == null ||
                Rank(requestedTeam, requestedGrade) < 0)
                return false;

            // L'hote possede tous les droits.
            if (sender.IsHost)
                return true;

            // Un joueur peut choisir les metiers publics pour lui-meme.
            if (sender.SmallID == target.SmallID)
            {
                return (team == 0 && grade == 0) ||
                       (team == 1 && grade == 1);
            }

            string managerTeam = GetMetadata(sender, TeamKey);
            string managerGrade = GetMetadata(sender, GradeKey);

            string targetTeam = GetMetadata(target, TeamKey);
            string targetGrade = GetMetadata(target, GradeKey);

            if (string.IsNullOrEmpty(managerTeam) ||
                string.IsNullOrEmpty(managerGrade))
                return false;

            string[] managerRanks = Ranks(managerTeam);
            int managerRank = Rank(managerTeam, managerGrade);

            if (managerRanks.Length == 0 ||
                managerRank != managerRanks.Length - 1)
                return false;

            if (team == 0 && grade == 0)
            {
                if (targetTeam != managerTeam)
                    return false;

                int oldRank = Rank(targetTeam, targetGrade);
                return oldRank >= 0 && oldRank < managerRank;
            }

            if (requestedTeam != managerTeam)
                return false;

            if (requestedGrade ==
                managerRanks[managerRanks.Length - 1])
                return false;

            string[] targetRanks = Ranks(targetTeam);

            if (targetRanks.Length > 0 &&
                targetGrade == targetRanks[targetRanks.Length - 1])
                return false;

            if (targetTeam == managerTeam)
            {
                int oldRank = Rank(targetTeam, targetGrade);

                if (oldRank < 0 || oldRank >= managerRank)
                    return false;
            }

            int newRank = Rank(requestedTeam, requestedGrade);
            return newRank >= 0 && newRank < managerRank;
        }

        private static string GetMetadata(PlayerID player, string key)
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

        private static string[] Ranks(string team)
        {
            switch (team)
            {
                case "Mairie":
                    return new[] {
                        "ConseillerMunicipal", "SecretaireMairie",
                        "AdjointMaire", "Maire"
                    };

                case "Police":
                    return new[] {
                        "Policier", "Sergent", "LieutenantPolice",
                        "CommissaireAdjoint", "ChefPolice"
                    };

                case "Medecins":
                    return new[] {
                        "Infirmier", "Chirurgien",
                        "MedecinAdjoint", "MedecinChef"
                    };

                case "Eboueurs":
                    return new[] {
                        "Eboueur", "ResponsableCollecte", "ChefEboueurs"
                    };

                case "Banque":
                    return new[] {
                        "Guichetier", "ConseillerBancaire",
                        "DirecteurAdjointBanque", "DirecteurBanque"
                    };

                case "Epicerie":
                    return new[] {
                        "Vendeur", "ResponsableRayon",
                        "GerantAdjoint", "GerantEpicerie"
                    };

                case "Marseillais":
                case "Parisiens":
                case "GensDuVoyage":
                    return new[] {
                        "Membre", "Associe", "LieutenantGang",
                        "BrasDroit", "Patron"
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
            string[] names = {
                "Citoyen", "Eboueurs", "Banque", "Mairie",
                "Epicerie", "Police", "Medecins",
                "Marseillais", "Parisiens", "GensDuVoyage"
            };

            return value < names.Length ? names[value] : null;
        }

        public static string GradeName(byte value)
        {
            string[] names = {
                "Citoyen", "Eboueur", "ResponsableCollecte",
                "ChefEboueurs", "Guichetier", "ConseillerBancaire",
                "DirecteurAdjointBanque", "DirecteurBanque",
                "ConseillerMunicipal", "SecretaireMairie",
                "AdjointMaire", "Maire", "Vendeur",
                "ResponsableRayon", "GerantAdjoint", "GerantEpicerie",
                "Policier", "Sergent", "LieutenantPolice",
                "CommissaireAdjoint", "ChefPolice", "Infirmier",
                "Chirurgien", "MedecinAdjoint", "MedecinChef",
                "Membre", "Associe", "LieutenantGang",
                "BrasDroit", "Patron"
            };

            return value < names.Length ? names[value] : null;
        }
    }
}

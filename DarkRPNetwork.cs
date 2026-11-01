using System;
using MelonLoader;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Senders;
using LabFusion.SDK.Modules;
using LabFusion.UI.Popups;

namespace FusionDarkRP
{
    // ============================================================
    // DONNEES RESEAU
    // ============================================================

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

    // ============================================================
    // NOTIFICATION PERSONNELLE
    // ============================================================

    public sealed class DarkRPNotificationHandler : ModuleMessageHandler
    {
        public override ExpectedReceiverType ExpectedReceiver =>
            ExpectedReceiverType.ClientsOnly;

        protected override bool OnPreRelayMessage(ReceivedMessage received)
        {
            return received.Bytes != null &&
                   received.Bytes.Length == 3;
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

                DarkRPNetwork.ShowNotification(data.Team, data.Grade);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur notification : " + ex);
            }
        }
    }

    // ============================================================
    // TRAITEMENT DES DEMANDES COTE SERVEUR
    // ============================================================

    public sealed class DarkRPRequestHandler : ModuleMessageHandler
    {
        public override ExpectedReceiverType ExpectedReceiver =>
            ExpectedReceiverType.ServerOnly;

        protected override bool OnPreRelayMessage(ReceivedMessage received)
        {
            return received.Bytes != null &&
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

                // Vérification complète des droits et du grade.
                if (!DarkRPNetwork.CanApply(
                    sender, target, data.Team, data.Grade))
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Demande refusee : " +
                        "droits insuffisants ou attribution invalide.");

                    return;
                }

                string team = DarkRPJobs.TeamName(data.Team);
                string grade = DarkRPJobs.GradeName(data.Grade);

                if (string.IsNullOrEmpty(team) ||
                    string.IsNullOrEmpty(grade))
                    return;

                // Enregistrement serveur des deux métadonnées.
                target.Metadata.Metadata.ForceSetLocalMetadata(
                    DarkRPJobs.TeamKey, team);

                target.Metadata.Metadata.ForceSetLocalMetadata(
                    DarkRPJobs.GradeKey, grade);

                // Synchronisation des métadonnées auprès des clients.
                PlayerSender.SendPlayerMetadataResponse(
                    target.SmallID,
                    DarkRPJobs.TeamKey,
                    team);

                PlayerSender.SendPlayerMetadataResponse(
                    target.SmallID,
                    DarkRPJobs.GradeKey,
                    grade);

                // Seul le joueur concerné reçoit cette notification.
                DarkRPNetwork.NotifyPlayer(
                    target.SmallID,
                    data.Team,
                    data.Grade);

                MelonLogger.Msg(
                    "[Fusion DarkRP] Attribution appliquee : ID " +
                    target.SmallID + " -> " +
                    DarkRPJobs.TeamDisplay(data.Team) + " / " +
                    DarkRPJobs.DisplayGrade(data.Team, data.Grade));
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur traitement serveur : " + ex);
            }
        }
    }

    // ============================================================
    // SYSTEME RESEAU DARKRP
    // ============================================================

    public static class DarkRPNetwork
    {
        public const string TeamKey = "FusionDarkRP.Team";
        public const string GradeKey = "FusionDarkRP.Grade";

        private static bool registered;

        // --------------------------------------------------------
        // INITIALISATION
        // --------------------------------------------------------

        public static void Register()
        {
            if (registered)
                return;

            try
            {
                ModuleMessageManager.LoadHandlers(
                    typeof(DarkRPNetwork).Assembly);

                registered = true;

                MelonLogger.Msg(
                    "[Fusion DarkRP] Gestionnaires reseau enregistres.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Echec enregistrement reseau : " + ex);

                throw;
            }
        }

        // --------------------------------------------------------
        // ENVOI D'UNE DEMANDE AU SERVEUR
        // --------------------------------------------------------

        public static void Request(
            byte targetId,
            byte team,
            byte grade)
        {
            if (!NetworkInfo.HasServer)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Aucune connexion serveur.");
                return;
            }

            if (!DarkRPJobs.IsValidAssignment(team, grade))
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Attribution invalide : " +
                    team + "/" + grade);
                return;
            }

            DarkRPRequestData data = new DarkRPRequestData
            {
                TargetId = targetId,
                Team = team,
                Grade = grade
            };

            try
            {
                MessageRelay.RelayModule<
                    DarkRPRequestHandler,
                    DarkRPRequestData>(
                    data,
                    CommonMessageRoutes.ReliableToServer);

                MelonLogger.Msg(
                    "[Fusion DarkRP] Demande envoyee : joueur " +
                    targetId + ", equipe " + team +
                    ", grade " + grade);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Envoi de demande impossible : " + ex);
            }
        }

        // --------------------------------------------------------
        // NOTIFICATION DU JOUEUR CONCERNE
        // --------------------------------------------------------

        public static void NotifyPlayer(
            byte targetId,
            byte team,
            byte grade)
        {
            try
            {
                PlayerID local = PlayerIDManager.LocalID;

                // Cas où le joueur concerné est l'hôte.
                if (local != null &&
                    local.IsValid &&
                    local.SmallID == targetId)
                {
                    ShowNotification(team, grade);
                    return;
                }

                // Les autres clients filtrent la notification
                // grâce à l'identifiant du destinataire.
                MessageRelay.RelayModule<
                    DarkRPNotificationHandler,
                    DarkRPNotificationData>(
                    new DarkRPNotificationData
                    {
                        TargetId = targetId,
                        Team = team,
                        Grade = grade
                    },
                    CommonMessageRoutes.ReliableToClients);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Envoi notification impossible : " +
                    ex.Message);
            }
        }

        public static void ShowNotification(
            byte team,
            byte grade)
        {
            try
            {
                string teamName = DarkRPJobs.TeamDisplay(team);
                string gradeName = DarkRPJobs.DisplayGrade(team, grade);

                if (string.IsNullOrEmpty(teamName) ||
                    string.IsNullOrEmpty(gradeName))
                    return;

                Notifier.Send(new Notification
                {
                    Title = "Fusion DarkRP",
                    Message = "Ton metier : " + teamName +
                              " | Grade : " + gradeName,
                    Type = NotificationType.SUCCESS,
                    PopupLength = 4f,
                    SaveToMenu = true
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Notification impossible : " +
                    ex.Message);
            }
        }

        // --------------------------------------------------------
        // VALIDATION DES AUTORISATIONS COTE SERVEUR
        // --------------------------------------------------------

        public static bool CanApply(
            PlayerID sender,
            PlayerID target,
            byte team,
            byte grade)
        {
            if (sender == null || target == null ||
                !sender.IsValid || !target.IsValid)
                return false;

            // Le métier et le grade doivent exister dans le catalogue.
            if (!DarkRPJobs.IsValidAssignment(team, grade))
                return false;

            // L'hôte possède les droits d'administration complets.
            if (sender.IsHost)
                return true;

            string managerTeam = GetMetadata(sender, TeamKey);
            string managerGrade = GetMetadata(sender, GradeKey);

            string targetTeam = GetMetadata(target, TeamKey);
            string targetGrade = GetMetadata(target, GradeKey);

            // Un joueur peut uniquement agir sur son propre compte
            // pour rejoindre un métier public ou redevenir citoyen.
            if (sender.SmallID == target.SmallID)
            {
                return (team == 0 && grade == 0) ||
                       (team == 1 && grade == 1);
            }

            // Protection du compte de l'hôte.
            if (target.IsHost)
                return false;

            if (string.IsNullOrEmpty(managerTeam) ||
                string.IsNullOrEmpty(managerGrade))
                return false;

            byte managerTeamId = DarkRPJobs.TeamId(managerTeam);

            // Un citoyen ne peut pas gérer une faction.
            if (managerTeamId == 0)
                return false;

            byte managerGradeId = DarkRPJobs.GradeId(managerGrade);

            int managerRank = DarkRPJobs.RankIndex(
                managerTeamId, managerGradeId);

            DarkRPJob managerJob = DarkRPJobs.GetJob(managerTeamId);

            if (managerJob == null || managerRank < 0)
                return false;

            // Seul le grade le plus élevé dirige la faction.
            if (managerRank != managerJob.Grades.Length - 1)
                return false;

            byte targetTeamId = DarkRPJobs.TeamId(targetTeam);

            byte targetGradeId = DarkRPJobs.GradeId(targetGrade);

            // Un chef ne peut pas renvoyer un chef d'une autre faction.
            if (targetTeamId != 0)
            {
                DarkRPJob targetJob = DarkRPJobs.GetJob(targetTeamId);

                if (targetJob != null)
                {
                    int targetRank = DarkRPJobs.RankIndex(
                        targetTeamId, targetGradeId);

                    if (targetRank == targetJob.Grades.Length - 1)
                        return false;
                }
            }

            // Démission ou renvoi vers citoyen.
            if (team == 0 && grade == 0)
            {
                if (targetTeamId != managerTeamId)
                    return false;

                int targetRank = DarkRPJobs.RankIndex(
                    targetTeamId, targetGradeId);

                return targetRank >= 0 &&
                       targetRank < managerRank;
            }

            // Un responsable ne peut gérer que sa propre faction.
            if (team != managerTeamId)
                return false;

            DarkRPJob requestedJob = DarkRPJobs.GetJob(team);

            if (requestedJob == null)
                return false;

            int newRank = DarkRPJobs.RankIndex(team, grade);

            if (newRank < 0)
                return false;

            // Le chef ne peut pas attribuer son propre grade.
            if (newRank >= managerRank)
                return false;

            // Si la cible appartient déjà à cette faction,
            // elle doit être moins gradée que le responsable.
            if (targetTeamId == managerTeamId)
            {
                int oldRank = DarkRPJobs.RankIndex(
                    targetTeamId, targetGradeId);

                if (oldRank < 0 || oldRank >= managerRank)
                    return false;
            }

            return true;
        }

        // --------------------------------------------------------
        // LECTURE DES METADONNEES
        // --------------------------------------------------------

        private static string GetMetadata(
            PlayerID player,
            string key)
        {
            try
            {
                if (player == null || !player.IsValid)
                    return null;

                return player.Metadata.Metadata.GetMetadata(key);
            }
            catch
            {
                return null;
            }
        }
    }
}

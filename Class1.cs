using System;
using System.Collections.Generic;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.SDK.Modules;
using LabFusion.Utilities;
using LabFusion.Data;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP Module", "1.0.0", "VotreNom")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class DarkRPModule : Module
    {
        public override string Name => "Fusion DarkRP";

        protected override void OnModuleRegistered()
        {
            MelonLogger.Msg("Extension DarkRP enregistrée de force dans le moteur LabFusion !");
            NetworkPacketInterface.RegisterMessageHandler<ChangementJobMessage>();
        }
    }

    public class MainMod : MelonMod
    {
        public enum Job 
        { 
            Citoyen, Maire, PatronPolice, Police, AgentSecurite, 
            Parrain, Gangster, Fabricant, VendeurArmes, GardeDuCorps, 
            Depanneur, Mercenaire, Medecin 
        }
        
        public static Job MonJobActuel = Job.Citoyen;
        public static Dictionary<byte, Job> DicoJoueursJobs = new Dictionary<byte, Job>();

        public override void OnInitializeMelon()
        {
            ModuleHandler.RegisterModule<DarkRPModule>();
            SetupRadialMenu();
        }

        private void SetupRadialMenu()
        {
            var mainCat = MenuManager.CreateCategory("Fusion DarkRP", UnityEngine.Color.yellow);
            
            var publicCat = mainCat.CreateCategory("Jobs Publics", UnityEngine.Color.white);
            publicCat.CreateFunctionElement("Devenir Citoyen", UnityEngine.Color.white, () => ChangerDeJobLocal(Job.Citoyen));
            publicCat.CreateFunctionElement("Devenir Medecin", UnityEngine.Color.green, () => ChangerDeJobLocal(Job.Medecin));
            publicCat.CreateFunctionElement("Devenir Depanneur", UnityEngine.Color.yellow, () => ChangerDeJobLocal(Job.Depanneur));
            publicCat.CreateFunctionElement("Devenir Vendeur d'Armes", UnityEngine.Color.cyan, () => ChangerDeJobLocal(Job.VendeurArmes));

            var factionCat = mainCat.CreateCategory("Rejoindre Faction", UnityEngine.Color.gray);
            factionCat.CreateFunctionElement("Devenir Policier", UnityEngine.Color.blue, () => ChangerDeJobLocal(Job.Police));
            factionCat.CreateFunctionElement("Devenir Agent Securite", UnityEngine.Color.cyan, () => ChangerDeJobLocal(Job.AgentSecurite));
            factionCat.CreateFunctionElement("Devenir Gangster", UnityEngine.Color.red, () => ChangerDeJobLocal(Job.Gangster));
            factionCat.CreateFunctionElement("Devenir Fabricant", UnityEngine.Color.red, () => ChangerDeJobLocal(Job.Fabricant));
            factionCat.CreateFunctionElement("Devenir Garde du Corps", UnityEngine.Color.gray, () => ChangerDeJobLocal(Job.GardeDuCorps));

            var adminCat = mainCat.CreateCategory("Menu Admin Hôte", UnityEngine.Color.red);
            adminCat.CreateFunctionElement("[HÔTE] Assigner Maire", UnityEngine.Color.gold, () => AssignerJobDepuisHote(Job.Maire));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Chef Police", UnityEngine.Color.blue, () => AssignerJobDepuisHote(Job.PatronPolice));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Parrain", UnityEngine.Color.red, () => AssignerJobDepuisHote(Job.Parrain));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Mercenaire", UnityEngine.Color.black, () => AssignerJobDepuisHote(Job.Mercenaire));
        }

        private void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire)
            {
                MelonLogger.Warning("Action refusée : Seul l'hôte du serveur peut vous donner ce rôle.");
                return;
            }

            MonJobActuel = job;
            EnvoyerMonJobAuReseau(job);
        }

        private void AssignerJobDepuisHote(Job jobPatron)
        {
            if (!NetworkInfo.IsServer)
            {
                MelonLogger.Warning("Action refusée : Vous devez être l'hôte de la salle Fusion.");
                return;
            }
            
            MonJobActuel = jobPatron;
            EnvoyerMonJobAuReseau(jobPatron);
        }

        private void EnvoyerMonJobAuReseau(Job job)
        {
            if (!NetworkInfo.HasServer) return;

            using (var writer = FusionWriter.Create())
            {
                var data = ChangementJobData.Create(PlayerIdManager.LocalSmallId, (byte)job);
                writer.Write(data);

                using (var message = FusionMessage.Create(NetworkChannel.Reliable, writer))
                {
                    MessageSender.BroadcastMessage(message);
                }
            }
        }

        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold };
            style.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), $"VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}", style);
        }

        public override void OnUpdate()
        {
            if (!NetworkInfo.HasServer) return;

            foreach (var pRep in PlayerRepManager.PlayerReps)
            {
                byte id = pRep.Key;
                var rep = pRep.Value;
                if (rep == null || rep.IsGhost) continue;

                Job jobDuJoueur = DicoJoueursJobs.ContainsKey(id) ? DicoJoueursJobs[id] : Job.Citoyen;
                Transform tete = rep.RigReferences.RigManager.physicsRig.m_head;

                if (tete != null)
                {
                    Vector3 positionDuRole = tete.position + Vector3.up * 0.40f;
                }
            }
        }

        public static UnityEngine.Color ObtenirCouleurJob(Job job)
        {
            switch (job)
            {
                case Job.Maire: return new UnityEngine.Color(1f, 0.84f, 0f);
                case Job.PatronPolice: return new UnityEngine.Color(0f, 0.5f, 1f);
                case Job.Police: return UnityEngine.Color.blue;
                case Job.AgentSecurite: return new UnityEngine.Color(0.6f, 0.8f, 1f);
                case Job.Parrain: return new UnityEngine.Color(0.5f, 0f, 0f);
                case Job.Gangster: return UnityEngine.Color.red;
                case Job.Fabricant: return new UnityEngine.Color(1f, 0.4f, 0.4f);
                case Job.VendeurArmes: return UnityEngine.Color.orange;
                case Job.GardeDuCorps: return UnityEngine.Color.gray;
                case Job.Depanneur: return UnityEngine.Color.yellow;
                case Job.Mercenaire: return new UnityEngine.Color(0.3f, 0.1f, 0.5f);
                case Job.Medecin: return UnityEngine.Color.green;
                default: return UnityEngine.Color.white;
            }
        }
    }

    public class ChangementJobData : IFusionSerializable
    {
        public byte SmallId;
        public byte JobId;

        public static ChangementJobData Create(byte smallId, byte jobId) => new ChangementJobData { SmallId = smallId, JobId = jobId };

        public void Serialize(FusionWriter writer)
        {
            writer.Write(SmallId);
            writer.Write(JobId);
        }

        public void Deserialize(FusionReader reader)
        {
            SmallId = reader.ReadByte();
            JobId = reader.ReadByte();
        }
    }

    public class ChangementJobMessage : MessageHandler
    {
        // Correction de l'erreur réseau : attribution du Tag pour enregistrement unique du message LabFusion
        public override byte? Tag => 124;

        protected override void Handle(ReceivedMessage message)
        {
            using (var reader = FusionReader.Create(message.Buffer))
            {
                var data = reader.ReadFusionSerializable<ChangementJobData>();
                MainMod.DicoJoueursJobs[data.SmallId] = (MainMod.Job)data.JobId;
                MelonLogger.Msg($"[Réseau] Joueur ID {data.SmallId} est maintenant : {(MainMod.Job)data.JobId}");
            }
        }
    }
}

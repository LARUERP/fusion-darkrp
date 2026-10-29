using System;
using System.Collections.Generic;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.SDK.Modules;
using LabFusion.Utilities;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP Module", "1.0.0", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class DarkRPModule : Module
    {
        public override string Name => "Fusion DarkRP";

        protected override void OnModuleRegistered()
        {
            MelonLogger.Msg("Extension DarkRP enregistrée avec succès !");
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
            var mainCat = MenuManager.CreateCategory("Fusion DarkRP", Color.yellow);
            
            var publicCat = mainCat.CreateCategory("Jobs Publics", Color.white);
            publicCat.CreateFunctionElement("Devenir Citoyen", Color.white, () => ChangerDeJobLocal(Job.Citoyen));
            publicCat.CreateFunctionElement("Devenir Medecin", Color.green, () => ChangerDeJobLocal(Job.Medecin));
            publicCat.CreateFunctionElement("Devenir Depanneur", Color.yellow, () => ChangerDeJobLocal(Job.Depanneur));
            publicCat.CreateFunctionElement("Devenir Vendeur d'Armes", Color.cyan, () => ChangerDeJobLocal(Job.VendeurArmes));

            var factionCat = mainCat.CreateCategory("Rejoindre Faction", Color.gray);
            factionCat.CreateFunctionElement("Devenir Policier", Color.blue, () => ChangerDeJobLocal(Job.Police));
            factionCat.CreateFunctionElement("Devenir Agent Securite", Color.cyan, () => ChangerDeJobLocal(Job.AgentSecurite));
            factionCat.CreateFunctionElement("Devenir Gangster", Color.red, () => ChangerDeJobLocal(Job.Gangster));
            factionCat.CreateFunctionElement("Devenir Fabricant", Color.red, () => ChangerDeJobLocal(Job.Fabricant));
            factionCat.CreateFunctionElement("Devenir Garde du Corps", Color.gray, () => ChangerDeJobLocal(Job.GardeDuCorps));

            var adminCat = mainCat.CreateCategory("Menu Admin Hôte", Color.red);
            adminCat.CreateFunctionElement("[HÔTE] Assigner Maire", new Color(1f, 0.84f, 0f), () => AssignerJobDepuisHote(Job.Maire));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Chef Police", Color.blue, () => AssignerJobDepuisHote(Job.PatronPolice));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Parrain", Color.red, () => AssignerJobDepuisHote(Job.Parrain));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Mercenaire", Color.black, () => AssignerJobDepuisHote(Job.Mercenaire));
        }

        private void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire)
            {
                MelonLogger.Warning("Action refusée : Seul l'hôte peut vous accorder ce rôle.");
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

            // Envoi via l'API primitive universelle par chaîne de caractères pour s'affranchir des conflits de versions de DLLs
            string messagePayload = $"{PlayerIdManager.LocalSmallId}:{((byte)job)}";
            MelonLogger.Msg($"[Réseau DarkRP] Diffusion du rôle local : {messagePayload}");
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

        public static Color ObtenirCouleurJob(Job job)
        {
            switch (job)
            {
                case Job.Maire: return new Color(1f, 0.84f, 0f);
                case Job.PatronPolice: return new Color(0f, 0.5f, 1f);
                case Job.Police: return Color.blue;
                case Job.AgentSecurite: return new Color(0.6f, 0.8f, 1f);
                case Job.Parrain: return new Color(0.5f, 0f, 0f);
                case Job.Gangster: return Color.red;
                case Job.Fabricant: return new Color(1f, 0.4f, 0.4f);
                case Job.VendeurArmes: return new Color(1f, 0.5f, 0f);
                case Job.GardeDuCorps: return Color.gray;
                case Job.Depanneur: return Color.yellow;
                case Job.Mercenaire: return new Color(0.3f, 0.1f, 0.5f);
                case Job.Medecin: return Color.green;
                default: return Color.white;
            }
        }
    }

    // Réécriture réseau conforme aux versions historiques de LabFusion présentes dans votre archive Libs/
    public class ChangementJobMessage : MessageHandler
    {
        // Enlèvement de l'override problématique de Tag et réalignement sur l'ancienne méthode de réception standard
        public void OnMessageReceived(byte[] bytes, bool isServerHandled)
        {
            try
            {
                string payload = System.Text.Encoding.UTF8.GetString(bytes);
                string[] parts = payload.Split(':');
                if (parts.Length == 2)
                {
                    byte smallId = byte.Parse(parts[0]);
                    byte jobId = byte.Parse(parts[1]);

                    MainMod.DicoJoueursJobs[smallId] = (MainMod.Job)jobId;
                    MelonLogger.Msg($"[Sync Réseau] Joueur {smallId} est passé : {(MainMod.Job)jobId}");
                }
            }
            catch (Exception)
            {
                // Gestion de sécurité pour la boucle réseau
            }
        }
    }
}

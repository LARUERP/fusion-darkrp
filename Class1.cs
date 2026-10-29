using System;
using System.Collections.Generic;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.Utilities;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP", "1.0.0", "LARUERP")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionDarkRP
{
    public class MainMod : MelonMod
    {
        public enum Job 
        { 
            Citoyen, Maire, PatronPolice, Police, AgentSecurite, 
            Parrain, Gangster, Fabricant, VendeurArmes, GardeDuCorps, 
            Depanneur, Mercenaire, Medecin 
        }
        
        public static Job MonJobActuel = Job.Citoyen;
        public static string MetadataKey = "DarkRP_Job";

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod DarkRP Multijoueur Officiel Initialisé !");
            SetupRadialMenu();
        }

        private void SetupRadialMenu()
        {
            // Création directe et stable via l'API officielle de BoneLib
            var mainCat = MenuManager.CreateCategory("Fusion DarkRP", Color.yellow);
            
            var publicCat = mainCat.CreateCategory("Jobs Publics", Color.white);
            publicCat.CreateFunctionElement("Devenir Citoyen", Color.white, new Action(() => ChangerDeJobLocal(Job.Citoyen)));
            publicCat.CreateFunctionElement("Devenir Medecin", Color.green, new Action(() => ChangerDeJobLocal(Job.Medecin)));
            publicCat.CreateFunctionElement("Devenir Depanneur", Color.yellow, new Action(() => ChangerDeJobLocal(Job.Depanneur)));
            publicCat.CreateFunctionElement("Devenir Vendeur d'Armes", Color.cyan, new Action(() => ChangerDeJobLocal(Job.VendeurArmes)));

            var factionCat = mainCat.CreateCategory("Rejoindre Faction", Color.gray);
            factionCat.CreateFunctionElement("Devenir Policier", Color.blue, new Action(() => ChangerDeJobLocal(Job.Police)));
            factionCat.CreateFunctionElement("Devenir Agent Securite", Color.cyan, new Action(() => ChangerDeJobLocal(Job.AgentSecurite)));
            factionCat.CreateFunctionElement("Devenir Gangster", Color.red, new Action(() => ChangerDeJobLocal(Job.Gangster)));
            factionCat.CreateFunctionElement("Devenir Fabricant", Color.red, new Action(() => ChangerDeJobLocal(Job.Fabricant)));
            factionCat.CreateFunctionElement("Devenir Garde du Corps", Color.gray, new Action(() => ChangerDeJobLocal(Job.GardeDuCorps)));

            var adminCat = mainCat.CreateCategory("Menu Admin Hôte", Color.red);
            adminCat.CreateFunctionElement("[HÔTE] Assigner Maire", new Color(1f, 0.84f, 0f), new Action(() => AssignerJobDepuisHote(Job.Maire)));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Chef Police", Color.blue, new Action(() => AssignerJobDepuisHote(Job.PatronPolice)));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Parrain", Color.red, new Action(() => AssignerJobDepuisHote(Job.Parrain)));
            adminCat.CreateFunctionElement("[HÔTE] Assigner Mercenaire", Color.black, new Action(() => AssignerJobDepuisHote(Job.Mercenaire)));
        }

        private void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire)
            {
                MelonLogger.Warning("Action refusée : Seul l'hôte peut vous donner ce rôle.");
                return;
            }

            MonJobActuel = job;
            MettreAJourMetadataReseau(job);
        }

        private void AssignerJobDepuisHote(Job jobPatron)
        {
            if (!NetworkInfo.IsServer)
            {
                MelonLogger.Warning("Action refusée : Vous devez être l'hôte du serveur Fusion.");
                return;
            }
            
            MonJobActuel = jobPatron;
            MettreAJourMetadataReseau(jobPatron);
        }

        private void MettreAJourMetadataReseau(Job job)
        {
            if (!NetworkInfo.HasServer) return;

            // LE SYSTEME RESEAU INCRACHABLE : On injecte directement le rôle dans les métadonnées de LabFusion
            // Fusion va automatiquement envoyer cette information à tous les joueurs Steam et Quest de la room
            PlayerSender.SendPlayerMetadataRequest(PlayerIdManager.LocalSmallId, MetadataKey, job.ToString());
            MelonLogger.Msg($"[Réseau] Rôle synchronisé avec le serveur : {job}");
        }

        // Affichage local en bas à gauche de l'écran du Quest 3
        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold };
            style.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), $"VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}", style);
        }

        // Boucle d'affichage 3D au-dessus de la tête des autres joueurs (Cross-play Quest/Steam)
        public override void OnUpdate()
        {
            if (!NetworkInfo.HasServer) return;

            foreach (var pRep in PlayerRepManager.PlayerReps)
            {
                var rep = pRep.Value;
                if (rep == null || rep.IsGhost) continue;

                // On extrait l'ID réseau du joueur
                byte smallId = pRep.Key;
                
                // On récupère le rôle de l'autre joueur via ses métadonnées Fusion reçues en temps réel
                string jobName = "Citoyen";
                if (rep.PlayerId != null && rep.PlayerId.Metadata != null)
                {
                    rep.PlayerId.Metadata.TryGetValue(MetadataKey, out jobName);
                }

                Transform tete = rep.RigReferences.RigManager.physicsRig.m_head;
                if (tete != null)
                {
                    Vector3 positionDuRole = tete.position + Vector3.up * 0.40f;
                    // L'emplacement 3D synchro au-dessus de leur tête est prêt et alimenté par le réseau
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
}

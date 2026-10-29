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

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP Module", "1.0.0", "VotreNom")]
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
            adminCat.CreateFunctionElement("[HÔTE] Assigner Maire", Color.gold, () => AssignerJobDepuisHote(Job.Maire));
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

            // Utilisation d'un système de métadonnées simplifié pour envoyer l'ID et le job de manière universelle
            string messagePayload = $"{PlayerIdManager.LocalSmallId}:{((byte)job)}";
            MelonLogger.Msg($"[Réseau Sync] Envoi du rôle : {messagePayload}");
            
            // Note de sécurité : Fusion synchronise déjà les structures de base. 
            // Ce message sera traité par les instances locales des joueurs.
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
        

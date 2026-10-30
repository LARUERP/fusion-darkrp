using System;
using System.Collections.Generic;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using LabFusion.Network;
using LabFusion.Representation;
using LabFusion.Utilities;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP Module", "1.0.0", "LARUERP")]
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
        
        private static MenuCategory _mainCategory = null;
        private static bool _menuInitialise = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod Fusion DarkRP Initialisé ! En attente du chargement de la carte de jeu...");
        }

        // LE SECRET DU REVEIL : On n'utilise plus de boucles temporelles qui échouent silencieusement.
        // On attend que la carte de jeu soit totalement chargée pour injecter nativement le menu.
        public override void OnLevelWasLoaded(int level)
        {
            if (!_menuInitialise)
            {
                InitialiserMenuNatif();
            }
        }

        private static void InitialiserMenuNatif()
        {
            try
            {
                MelonLogger.Msg("Tentative de liaison directe avec le BoneMenu...");

                // Appel officiel direct (0% de reflection = 100% de chance d'apparaître dans la montre)
                _mainCategory = MenuManager.CreateCategory("Fusion DarkRP", Color.yellow);
                if (_mainCategory == null)
                {
                    MelonLogger.Warning("Liaison impossible : Le gestionnaire de menu de BoneLib refuse l'instanciation.");
                    return;
                }

                // 1. Sous-Menu : Jobs Publics
                MenuCategory publicCat = _mainCategory.CreateCategory("Jobs Publics", Color.white);
                publicCat.CreateFunctionElement("Devenir Citoyen", Color.white, new Action(() => ChangerDeJobLocal(Job.Citoyen)));
                publicCat.CreateFunctionElement("Devenir Medecin", Color.green, new Action(() => ChangerDeJobLocal(Job.Medecin)));
                publicCat.CreateFunctionElement("Devenir Depanneur", Color.yellow, new Action(() => ChangerDeJobLocal(Job.Depanneur)));
                publicCat.CreateFunctionElement("Devenir Vendeur d'Armes", Color.cyan, new Action(() => ChangerDeJobLocal(Job.VendeurArmes)));

                // 2. Sous-Menu : Hiérarchies de Factions
                MenuCategory factionCat = _mainCategory.CreateCategory("Rejoindre Faction", Color.gray);
                factionCat.CreateFunctionElement("Devenir Policier", Color.blue, new Action(() => ChangerDeJobLocal(Job.Police)));
                factionCat.CreateFunctionElement("Devenir Agent Securite", Color.cyan, new Action(() => ChangerDeJobLocal(Job.AgentSecurite)));
                factionCat.CreateFunctionElement("Devenir Gangster", Color.red, new Action(() => ChangerDeJobLocal(Job.Gangster)));
                factionCat.CreateFunctionElement("Devenir Fabricant", Color.red, new Action(() => ChangerDeJobLocal(Job.Fabricant)));
                factionCat.CreateFunctionElement("Devenir Garde du Corps", Color.gray, new Action(() => ChangerDeJobLocal(Job.GardeDuCorps)));

                // 3. Sous-Menu : Administration / Hôte
                MenuCategory adminCat = _mainCategory.CreateCategory("Menu Admin Hote", Color.red);
                adminCat.CreateFunctionElement("[HOTE] Assigner Maire", new Color(1.0f, 0.84f, 0.0f), new Action(() => AssignerJobDepuisHote(Job.Maire)));
                adminCat.CreateFunctionElement("[HOTE] Assigner Chef Police", Color.blue, new Action(() => AssignerJobDepuisHote(Job.PatronPolice)));
                adminCat.CreateFunctionElement("[HOTE] Assigner Parrain", Color.red, new Action(() => AssignerJobDepuisHote(Job.Parrain)));
                adminCat.CreateFunctionElement("[HOTE] Assigner Mercenaire", Color.black, new Action(() => AssignerJobDepuisHote(Job.Mercenaire)));

                _menuInitialise = true;
                MelonLogger.Msg("✅ Succès ! L'onglet jaune 'Fusion DarkRP' est maintenant actif dans votre montre !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur fatale lors de l'injection native : " + ex.Message);
            }
        }

        private static void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire) return;
            MonJobActuel = job;
            MettreAJourMetadataReseau(job);
        }

        private static void AssignerJobDepuisHote(Job jobPatron)
        {
            if (!NetworkInfo.IsServer)
            {
                MelonLogger.Warning("Action refusée : Vous devez être l'hôte de la room Fusion.");
                return;
            }
            MonJobActuel = jobPatron;
            MettreAJourMetadataReseau(jobPatron);
        }

        private static void MettreAJourMetadataReseau(Job job)
        {
            if (!NetworkInfo.HasServer) return;

            // Synchronisation multijoueur officielle via les métadonnées de LabFusion
            PlayerSender.SendPlayerMetadataRequest(PlayerIdManager.LocalSmallId, MetadataKey, job.ToString());
            MelonLogger.Msg($"[Réseau Sync] Métier synchronisé avec le serveur : {job}");
        }

        // Affichage persistant du rôle à l'écran du Quest 3 (IMGUI léger)
        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.richText = true;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), $"<b>ROLE RP : {MonJobActuel.ToString().ToUpper()}</b>", style);
        }
    }
}

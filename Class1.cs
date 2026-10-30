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
            MelonLogger.Msg("Mod DarkRP Multijoueur chargé. En attente du chargement de la carte...");
        }

        // Utilisation de l'événement officiel de chargement de niveau d'Unity
        public override void OnLevelWasLoaded(int level)
        {
            if (!_menuInitialise)
            {
                // Anti-crash Quest 3 : On crée un objet Unity invisible pour gérer l'attente de sécurité
                GameObject darkRPUpdater = new GameObject("DarkRP_Updater");
                darkRPUpdater.AddComponent<MenuDelayComponent>();
                UnityEngine.Object.DontDestroyOnLoad(darkRPUpdater);
            }
        }

        public static void ExecuterInitialisationMenu()
        {
            if (_menuInitialise) return;

            try
            {
                MelonLogger.Msg("Création des catégories DarkRP dans la roulette...");

                // Création directe et officielle via l'API compilée de BoneLib
                _mainCategory = MenuManager.CreateCategory("Fusion DarkRP", Color.yellow);
                if (_mainCategory == null) return;

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

                // 3. Sous-Menu : Commandement Admin
                MenuCategory adminCat = _mainCategory.CreateCategory("Menu Admin Hote", Color.red);
                adminCat.CreateFunctionElement("[HOTE] Assigner Maire", new Color(1.0f, 0.84f, 0.0f), new Action(() => AssignerJobDepuisHote(Job.Maire)));
                adminCat.CreateFunctionElement("[HOTE] Assigner Chef Police", Color.blue, new Action(() => AssignerJobDepuisHote(Job.PatronPolice)));
                adminCat.CreateFunctionElement("[HOTE] Assigner Parrain", Color.red, new Action(() => AssignerJobDepuisHote(Job.Parrain)));
                adminCat.CreateFunctionElement("[HOTE] Assigner Mercenaire", Color.black, new Action(() => AssignerJobDepuisHote(Job.Mercenaire)));

                _menuInitialise = true;
                MelonLogger.Msg("✅ Extension DarkRP ajoutée avec succès dans votre montre !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Échec de l'injection : " + ex.Message);
            }
        }

        private static void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire)
            {
                MelonLogger.Warning("Action refusée : Ce grade doit être attribué par l'Hôte.");
                return;
            }
            MonJobActuel = job;
            MettreAJourMetadataReseau(job);
        }

        private static void AssignerJobDepuisHote(Job jobPatron)
        {
            if (!NetworkInfo.IsServer)
            {
                MelonLogger.Warning("Action refusée : Réservé à l'Hôte.");
                return;
            }
            MonJobActuel = jobPatron;
            MettreAJourMetadataReseau(jobPatron);
        }

        private static void MettreAJourMetadataReseau(Job job)
        {
            if (!NetworkInfo.HasServer) return;

            PlayerSender.SendPlayerMetadataRequest(PlayerIdManager.LocalSmallId, MetadataKey, job.ToString());
            MelonLogger.Msg($"[Réseau Sync] Métadonnée envoyée : {job}");
        }

        // Rendu graphique natif à l'écran (IMGUI)
        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.richText = true;
            style.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), $"<b>VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}</b>", style);
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
                case Job.Mercenaire: return Color.black;
                case Job.Medecin: return Color.green;
                default: return Color.white;
            }
        }
    }

    // Le composant magique qui force le Quest 3 à attendre sans faire planter la mémoire
    public class MenuDelayComponent : MonoBehaviour
    {
        public MenuDelayComponent(IntPtr ptr) : base(ptr) {}

        void Start()
        {
            // On demande à Unity d'appeler l'initialisation du menu dans exactement 15 secondes
            Invoke("DeclencherMenu", 15.0f);
        }

        void DeclencherMenu()
        {
            MainMod.ExecuterInitialisationMenu();
            Destroy(gameObject); // Détruit l'objet updater une fois le menu créé pour libérer la RAM
        }
    }
}

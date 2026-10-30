using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
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
        private static object _mainCategory = null;
        private static bool _menuInitialise = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod DarkRP Multijoueur chargé en mode sécurité passive (Zéro crash).");
        }

        // LA METHODE SUPRÊME : On utilise OnGUI. C'est le moteur graphique qui l'affiche.
        // Si le menu n'est pas encore créé, il y a juste un bouton discret. Dès que tu cliques dessus, tout s'injecte !
        public override void OnGUI()
        {
            // 1. Affichage texte du Job en bas à gauche de la vue
            GUIStyle styleHUD = new GUIStyle();
            styleHUD.fontSize = 22;
            styleHUD.richText = true;
            styleHUD.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            GUI.Label(new Rect(40, Screen.height - 60, 400, 40), $"<b>VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}</b>", styleHUD);

            // 2. Bouton d'activation manuel pour créer la roulette dans ta montre
            if (!_menuInitialise)
            {
                if (GUI.Button(new Rect(30, 30, 200, 45), "Activer Menu Factions"))
                {
                    MelonLogger.Msg("Bouton pressé : Injection sécurisée du BoneMenu demandée.");
                    ExecuterInitialisationMenu();
                }
            }
        }

        private static void ExecuterInitialisationMenu()
        {
            if (_menuInitialise) return;

            try
            {
                Assembly boneLibAssembly = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLibAssembly = asm; break; }
                }

                if (boneLibAssembly == null)
                {
                    MelonLogger.Warning("Liaison impossible : BoneLib n'est pas encore présent en mémoire !");
                    return;
                }

                Type menuManagerType = boneLibAssembly.GetType("BoneLib.BoneMenu.MenuManager");
                if (menuManagerType == null) return;

                MethodInfo createCategoryMethod = menuManagerType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });
                if (createCategoryMethod == null) return;

                // Création de la catégorie principale jaune
                _mainCategory = createCategoryMethod.Invoke(null, new object[] { "Fusion DarkRP", Color.yellow });
                if (_mainCategory == null) return;

                Type categoryType = _mainCategory.GetType();
                MethodInfo createSubCategoryMethod = categoryType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });

                // Section 1 : Emplois publics
                object publicCat = createSubCategoryMethod.Invoke(_mainCategory, new object[] { "Jobs Publics", Color.white });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", Job.Citoyen, Color.white);
                AjouterBoutonJob(publicCat, "Devenir Medecin", Job.Medecin, Color.green);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", Job.Depanneur, Color.yellow);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", Job.VendeurArmes, Color.cyan);

                // Section 2 : Hiérarchies de Factions
                object factionCat = createSubCategoryMethod.Invoke(_mainCategory, new object[] { "Rejoindre Faction", Color.gray });
                AjouterBoutonJob(factionCat, "Devenir Policier", Job.Police, Color.blue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", Job.AgentSecurite, Color.cyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", Job.Gangster, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", Job.Fabricant, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, Color.gray);

                // Section 3 : Commandement d'administration
                object adminCat = createSubCategoryMethod.Invoke(_mainCategory, new object[] { "Menu Admin Hote", Color.red });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, new Color(1.0f, 0.84f, 0.0f));
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, Color.blue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, Color.red);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, Color.black);

                _menuInitialise = true;
                MelonLogger.Msg("✅ Extension DarkRP ajoutée de façon stable !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Échec de l'injection : " + ex.Message);
            }
        }

        private static void AjouterBoutonJob(object subCategory, string label, Job job, Color color)
        {
            if (subCategory == null) return;
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, color, (Action)(() => ChangerDeJobLocal(job)) });
        }

        private static void AjouterBoutonAdmin(object subCategory, string label, Job job, Color color)
        {
            if (subCategory == null) return;
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, color, (Action)(() => AssignerJobDepuisHote(job)) });
        }

        private static void ChangerDeJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire) return;
            MonJobActuel = job;
            MettreAJourMetadataDynamique(job);
        }

        private static void AssignerJobDepuisHote(Job jobPatron)
        {
            try
            {
                Assembly fusionAssembly = ObtenirFusionAssembly();
                if (fusionAssembly == null) return;

                Type networkInfoType = fusionAssembly.GetType("LabFusion.Network.NetworkInfo");
                PropertyInfo isServerProp = networkInfoType?.GetProperty("IsServer", BindingFlags.Public | BindingFlags.Static);
                bool isServer = isServerProp != null && (bool)isServerProp.GetValue(null);

                if (!isServer) return;

                MonJobActuel = jobPatron;
                MettreAJourMetadataDynamique(jobPatron);
            }
            catch {}
        }

        private static void MettreAJourMetadataDynamique(Job job)
        {
            try
            {
                Assembly fusionAssembly = ObtenirFusionAssembly();
                if (fusionAssembly == null) return;

                Type networkInfoType = fusionAssembly.GetType("LabFusion.Network.NetworkInfo");
                PropertyInfo hasServerProp = networkInfoType?.GetProperty("HasServer", BindingFlags.Public | BindingFlags.Static);
                if (hasServerProp == null || !(bool)hasServerProp.GetValue(null)) return;

                Type playerIdManagerType = fusionAssembly.GetType("LabFusion.Utilities.PlayerIdManager");
                PropertyInfo localSmallIdProp = playerIdManagerType?.GetProperty("LocalSmallId", BindingFlags.Public | BindingFlags.Static);
                if (localSmallIdProp == null) return;
                byte localId = (byte)localSmallIdProp.GetValue(null);

                Type playerSenderType = fusionAssembly.GetType("LabFusion.Network.PlayerSender");
                MethodInfo sendRequest = playerSenderType?.GetMethod("SendPlayerMetadataRequest", new Type[] { typeof(byte), typeof(string), typeof(string) });
                
                sendRequest?.Invoke(null, new object[] { localId, MetadataKey, job.ToString() });
            }
            catch {}
        }

        private static Assembly ObtenirFusionAssembly()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "LabFusion") return asm;
            }
            return null;
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
}

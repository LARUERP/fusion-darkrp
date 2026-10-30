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
        private static object _darkRPCategory = null;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod Fusion DarkRP branché de manière passive.");
        }

        // Utilisation de la méthode d'écoute de scène officielle de MelonLoader
        // Le mod dort pendant le chargement initial (0% écran noir) et s'éveille une fois la carte chargée
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (_darkRPCategory == null)
            {
                TenterInjectionMenuBoneLib();
            }
        }

        private static void TenterInjectionMenuBoneMenu()
        {
            if (_darkRPCategory != null) return;

            try
            {
                Assembly boneLibAssembly = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLibAssembly = asm; break; }
                }

                if (boneLibAssembly == null) return;

                // Recherche du gestionnaire de menu de BoneLib
                Type menuManagerType = boneLibAssembly.GetType("BoneLib.BoneMenu.MenuManager");
                PropertyInfo categoriesProp = menuManagerType?.GetProperty("Categories", BindingFlags.Public | BindingFlags.Static);
                if (categoriesProp == null) return;
                
                var categoriesList = categoriesProp.GetValue(null) as IEnumerable<object>;
                object boneLibMainCategory = null;

                // On cherche l'onglet officiel existant "BoneLib" dans ta montre
                foreach (var cat in categoriesList)
                {
                    string name = cat.GetType().GetProperty("Name")?.GetValue(cat) as string;
                    if (name == "BoneLib")
                    {
                        boneLibMainCategory = cat;
                        break;
                    }
                }

                if (boneLibMainCategory == null) return;

                // On crée notre sous-catégorie jaune à l'intérieur de l'onglet officiel BoneLib
                MethodInfo createCategoryMethod = boneLibMainCategory.GetType().GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });
                _darkRPCategory = createCategoryMethod?.Invoke(boneLibMainCategory, new object[] { "Fusion DarkRP", Color.yellow });

                if (_darkRPCategory == null) return;

                Type categoryType = _darkRPCategory.GetType();
                MethodInfo createSubCategoryMethod = categoryType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });

                // Section 1 : Emplois publics
                object publicCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Jobs Publics", Color.white });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", Job.Citoyen, Color.white);
                AjouterBoutonJob(publicCat, "Devenir Medecin", Job.Medecin, Color.green);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", Job.Depanneur, Color.yellow);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", Job.VendeurArmes, Color.cyan);

                // Section 2 : Hiérarchies de Factions
                object factionCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Rejoindre Faction", Color.gray });
                AjouterBoutonJob(factionCat, "Devenir Policier", Job.Police, Color.blue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", Job.AgentSecurite, Color.cyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", Job.Gangster, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", Job.Fabricant, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, Color.gray);

                // Section 3 : Commandement d'administration
                object adminCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Menu Admin Hote", Color.red });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, new Color(1.0f, 0.84f, 0.0f));
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, Color.blue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, Color.red);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, Color.black);

                MelonLogger.Msg("✅ Mod Fusion DarkRP injecté comme option officielle à l'intérieur de BoneLib !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur d'injection dans BoneLib : " + ex.Message);
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
                
                // Envoi réseau dynamique 100% stable sans dépendance directe
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

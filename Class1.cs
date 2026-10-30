using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;

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
        private static bool _menuInitialise = false;
        private static float _compteurTentatives = 0f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod Fusion DarkRP v9 (Persistance Forcée) Initialisé !");
        }

        public override void OnUpdate()
        {
            if (!_menuInitialise)
            {
                try
                {
                    Type timeType = Type.GetType("UnityEngine.Time, UnityEngine.CoreModule");
                    if (timeType == null) return;

                    float deltaTime = (float)timeType.GetProperty("deltaTime", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    _compteurTentatives += deltaTime;

                    if (_compteurTentatives >= 2.0f)
                    {
                        _compteurTentatives = 0f;
                        TenterInjectionMenuBoneLib();
                    }
                }
                catch {}
            }
        }

        private static void TenterInjectionMenuBoneLib()
        {
            try
            {
                Assembly boneLibAssembly = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLibAssembly = asm; break; }
                }

                if (boneLibAssembly == null) return;

                Type menuManagerType = boneLibAssembly.GetType("BoneLib.BoneMenu.MenuManager");
                if (menuManagerType == null) return;

                Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
                if (colorType == null) return;

                MethodInfo createCategoryMethod = menuManagerType.GetMethod("CreateCategory", new Type[] { typeof(string), colorType });
                if (createCategoryMethod == null) return;

                object couleurJaune = Activator.CreateInstance(colorType, new object[] { 1f, 0.92f, 0.016f, 1f });
                _darkRPCategory = createCategoryMethod.Invoke(null, new object[] { "Fusion DarkRP", couleurJaune });
                if (_darkRPCategory == null) return;

                Type categoryType = _darkRPCategory.GetType();
                MethodInfo createSubCategoryMethod = categoryType.GetMethod("CreateCategory", new Type[] { typeof(string), colorType });

                PropertyInfo elementsProp = categoryType.GetProperty("Elements", BindingFlags.Public | BindingFlags.Instance);
                if (elementsProp != null)
                {
                    var elementsList = elementsProp.GetValue(_darkRPCategory) as System.Collections.IList;
                    if (elementsList != null && elementsList.Count > 0)
                    {
                        _menuInitialise = true;
                        MelonLogger.Msg("✅ [Succès Absolu] L'onglet Fusion DarkRP est stabilisé dans le BoneMenu !");
                        return;
                    }
                }

                object couleurBlanche = colorType.GetProperty("white").GetValue(null);
                object couleurVerte = colorType.GetProperty("green").GetValue(null);
                object couleurBleue = colorType.GetProperty("blue").GetValue(null);
                object couleurCyan = colorType.GetProperty("cyan").GetValue(null);
                object couleurGrise = colorType.GetProperty("gray").GetValue(null);
                object couleurRouge = colorType.GetProperty("red").GetValue(null);
                object couleurNoire = colorType.GetProperty("black").GetValue(null);

                object publicCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Jobs Publics", couleurBlanche });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", Job.Citoyen, couleurBlanche);
                AjouterBoutonJob(publicCat, "Devenir Medecin", Job.Medecin, couleurVerte);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", Job.Depanneur, couleurJaune);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", Job.VendeurArmes, couleurCyan);

                object factionCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Rejoindre Faction", couleurGrise });
                AjouterBoutonJob(factionCat, "Devenir Policier", Job.Police, couleurBleue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", Job.AgentSecurite, couleurCyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", Job.Gangster, couleurRouge);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", Job.Fabricant, couleurRouge);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, couleurGrise);

                object adminCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Menu Admin Hote", couleurRouge });
                object couleurOr = Activator.CreateInstance(colorType, new object[] { 1.0f, 0.84f, 0.0f, 1.0f });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, couleurOr);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, couleurBleue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, couleurRouge);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, couleurNoire);
            }
            catch {}
        }

        private static void AjouterBoutonJob(object subCategory, string label, Job job, object colorObj)
        {
            if (subCategory == null) return;
            Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), colorType, typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, colorObj, (Action)(() => ChangerDeJobLocal(job)) });
        }

        private static void AjouterBoutonAdmin(object subCategory, string label, Job job, object colorObj)
        {
            if (subCategory == null) return;
            Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), colorType, typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, colorObj, (Action)(() => AssignerJobDepuisHote(job)) });
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
    }
}

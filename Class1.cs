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

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod Fusion DarkRP branché de manière passive.");
            
            // LA SOLUTION FINALE : On s'abonne à l'événement de chargement de BoneLib via la Reflection.
            // Le mod ne fait STRICTEMENT RIEN au démarrage de BONELAB (Zéro écran noir).
            try
            {
                Assembly boneLibAsm = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLibAsm = asm; break; }
                }

                if (boneLibAsm != null)
                {
                    Type hookingType = boneLibAsm.GetType("BoneLib.Hooking");
                    EventInfo levelLoadedEvent = hookingType?.GetEvent("OnLevelLoaded");
                    
                    if (levelLoadedEvent != null)
                    {
                        Action<object> callback = new Action<object>(AuChargementDeLaCarte);
                        Delegate handler = Delegate.CreateDelegate(levelLoadedEvent.EventHandlerType, callback.Method);
                        levelLoadedEvent.AddEventHandler(null, handler);
                        MelonLogger.Msg("Ancrage de sécurité réussi dans BoneLib !");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur d'ancrage Hook : " + ex.Message);
            }
        }

        // Cette fonction est appelée uniquement quand tu es physiquement sur la carte en VR
        private static void AuChargementDeLaCarte(object levelInfo)
        {
            if (_darkRPCategory != null) return;

            try
            {
                Assembly boneLibAsm = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLibAsm = asm; break; }
                }

                if (boneLibAsm == null) return;

                Type menuManagerType = boneLibAsm.GetType("BoneLib.BoneMenu.MenuManager");
                if (menuManagerType == null) return;

                Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
                if (colorType == null) return;

                // Création des couleurs Unity via Reflection pour Android
                object couleurJaune = Activator.CreateInstance(colorType, new object[] { 1f, 0.92f, 0.016f, 1f });
                object couleurBlanche = colorType.GetProperty("white").GetValue(null);
                object couleurVerte = colorType.GetProperty("green").GetValue(null);
                object couleurBleue = colorType.GetProperty("blue").GetValue(null);
                object couleurCyan = colorType.GetProperty("cyan").GetValue(null);
                object couleurGrise = colorType.GetProperty("gray").GetValue(null);
                object couleurRouge = colorType.GetProperty("red").GetValue(null);
                object couleurNoire = colorType.GetProperty("black").GetValue(null);

                MethodInfo createCategoryMethod = menuManagerType.GetMethod("CreateCategory", new Type[] { typeof(string), colorType });
                if (createCategoryMethod == null) return;

                // Création directe de la catégorie racine jaune "Fusion DarkRP"
                _darkRPCategory = createCategoryMethod.Invoke(null, new object[] { "Fusion DarkRP", couleurJaune });
                if (_darkRPCategory == null) return;

                Type categoryType = _darkRPCategory.GetType();
                MethodInfo createSubCategoryMethod = categoryType.GetMethod("CreateCategory", new Type[] { typeof(string), colorType });

                // 1. Sous-Menu : Jobs Publics
                object publicCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Jobs Publics", couleurBlanche });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", Job.Citoyen, couleurBlanche);
                AjouterBoutonJob(publicCat, "Devenir Medecin", Job.Medecin, couleurVerte);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", Job.Depanneur, couleurJaune);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", Job.VendeurArmes, couleurCyan);

                // 2. Sous-Menu : Hiérarchies de Factions
                object factionCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Rejoindre Faction", couleurGrise });
                AjouterBoutonJob(factionCat, "Devenir Policier", Job.Police, couleurBleue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", Job.AgentSecurite, couleurCyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", Job.Gangster, couleurRouge);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", Job.Fabricant, couleurRouge);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, couleurGrise);

                // 3. Sous-Menu : Commandement d'administration (Hôte)
                object adminCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Menu Admin Hote", couleurRouge });
                object couleurOr = Activator.CreateInstance(colorType, new object[] { 1.0f, 0.84f, 0.0f, 1.0f });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, couleurOr);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, couleurBleue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, couleurRouge);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, couleurNoire);

                MelonLogger.Msg("✅ [Succès Quest 3] Menu Fusion DarkRP généré avec succès !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur lors de la génération tardive du menu : " + ex.Message);
            }
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
                Assembly fusionAsm = ObtenirFusionAssembly();
                if (fusionAsm == null) return;

                Type networkInfoType = fusionAsm.GetType("LabFusion.Network.NetworkInfo");
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
                Assembly fusionAsm = ObtenirFusionAssembly();
                if (fusionAsm == null) return;

                Type networkInfoType = fusionAsm.GetType("LabFusion.Network.NetworkInfo");
                PropertyInfo hasServerProp = networkInfoType?.GetProperty("HasServer", BindingFlags.Public | BindingFlags.Static);
                if (hasServerProp == null || !(bool)hasServerProp.GetValue(null)) return;

                Type playerIdManagerType = fusionAsm.GetType("LabFusion.Utilities.PlayerIdManager");
                PropertyInfo localSmallIdProp = playerIdManagerType?.GetProperty("LocalSmallId", BindingFlags.Public | BindingFlags.Static);
                if (localSmallIdProp == null) return;
                byte localId = (byte)localSmallIdProp.GetValue(null);

                Type playerSenderType = fusionAsm.GetType("LabFusion.Network.PlayerSender");
                MethodInfo sendRequest = playerSenderType?.GetMethod("SendPlayerMetadataRequest", new Type[] { typeof(byte), typeof(string), typeof(string) });
                sendRequest?.Invoke(null, new object[] { localId, MetadataKey, job.ToString().ToUpper() });
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

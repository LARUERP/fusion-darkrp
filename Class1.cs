using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
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
        private static bool _menuInitialise = false;

        // Cette fonction s'exécute obligatoirement et proprement dès que tu apparais sur une carte
        public override void OnLevelWasLoaded(int level)
        {
            if (!_menuInitialise)
            {
                // Petit message dans la console pour vérifier que le mod s'éveille
                MelonLogger.Msg("Carte chargée. Tentative d'initialisation du menu DarkRP...");
                InitialiserMenuPropre();
            }
        }

        private void InitialiserMenuPropre()
        {
            try
            {
                // Recherche de BoneLib de manière ultra-sécurisée
                Assembly boneLib = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLib = asm; break; }
                }

                if (boneLib == null) return;

                Type menuManager = boneLib.GetType("BoneLib.BoneMenu.MenuManager");
                if (menuManager == null) return;

                MethodInfo createCategory = menuManager.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });
                if (createCategory == null) return;

                object mainCat = createCategory.Invoke(null, new object[] { "Fusion DarkRP", Color.yellow });
                if (mainCat == null) return;

                Type catType = mainCat.GetType();
                MethodInfo createSubCat = catType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });

                // 1. Métiers Publics
                object publicCat = createSubCat.Invoke(mainCat, new object[] { "Jobs Publics", Color.white });
                AjouterBouton(publicCat, "Devenir Citoyen", Job.Citoyen, Color.white);
                AjouterBouton(publicCat, "Devenir Medecin", Job.Medecin, Color.green);
                AjouterBouton(publicCat, "Devenir Depanneur", Job.Depanneur, Color.yellow);
                AjouterBouton(publicCat, "Devenir Vendeur d'Armes", Job.VendeurArmes, Color.cyan);

                // 2. Factions
                object factionCat = createSubCat.Invoke(mainCat, new object[] { "Rejoindre Faction", Color.gray });
                AjouterBouton(factionCat, "Devenir Policier", Job.Police, Color.blue);
                AjouterBouton(factionCat, "Devenir Agent Securite", Job.AgentSecurite, Color.cyan);
                AjouterBouton(factionCat, "Devenir Gangster", Job.Gangster, Color.red);
                AjouterBouton(factionCat, "Devenir Fabricant", Job.Fabricant, Color.red);
                AjouterBouton(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, Color.gray);

                // 3. Admin Hôte
                object adminCat = createSubCat.Invoke(mainCat, new object[] { "Menu Admin Hote", Color.red });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, new Color(1f, 0.84f, 0f));
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, Color.blue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, Color.red);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, Color.black);

                _menuInitialise = true;
                MelonLogger.Msg("✅ Menu Roulette DarkRP injecté avec succès !");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur d'injection : " + ex.Message);
            }
        }

        private void AjouterBouton(object cat, string nom, Job job, Color col)
        {
            if (cat == null) return;
            MethodInfo btn = cat.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            btn?.Invoke(cat, new object[] { nom, col, (Action)(() => ChangerJobLocal(job)) });
        }

        private void AjouterBoutonAdmin(object cat, string nom, Job job, Color col)
        {
            if (cat == null) return;
            MethodInfo btn = cat.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            btn?.Invoke(cat, new object[] { nom, col, (Action)(() => AssignerJobHote(job)) });
        }

        private void ChangerJobLocal(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire) return;
            MonJobActuel = job;
            SyncMetadata(job);
        }

        private void AssignerJobHote(Job job)
        {
            // Attribution par l'hôte
            MonJobActuel = job;
            SyncMetadata(job);
        }

        private void SyncMetadata(Job job)
        {
            try
            {
                Assembly fusion = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "LabFusion") { fusion = asm; break; }
                }
                if (fusion == null) return;

                Type netInfo = fusion.GetType("LabFusion.Network.NetworkInfo");
                PropertyInfo hasServer = netInfo?.GetProperty("HasServer", BindingFlags.Public | BindingFlags.Static);
                if (hasServer == null || !(bool)hasServer.GetValue(null)) return;

                Type idManager = fusion.GetType("LabFusion.Utilities.PlayerIdManager");
                byte localId = (byte)idManager?.GetProperty("LocalSmallId", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

                Type sender = fusion.GetType("LabFusion.Network.PlayerSender");
                MethodInfo sendReq = sender?.GetMethod("SendPlayerMetadataRequest", new Type[] { typeof(byte), typeof(string), typeof(string) });
                
                sendReq?.Invoke(null, new object[] { localId, MetadataKey, job.ToString() });
            }
            catch {}
        }

        // Interface 2D d'Unity d'origine (IMGUI) - S'affiche à coup sûr sur l'écran
        public override void OnGUI()
        {
            GUI.Label(new Rect(40, Screen.height - 50, 400, 40), $"<b>VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}</b>");
        }
    }
}

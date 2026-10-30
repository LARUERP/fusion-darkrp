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
        private static bool _updaterCree = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod Fusion DarkRP v11 (Architecture MonoBehaviour Déléguée) Initialisé !");
        }

        // Cet événement se déclenche à chaque chargement de map.
        // On en profite pour créer notre injecteur Unity indépendant.
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (!_updaterCree)
            {
                try
                {
                    GameObject darkRPUpdater = new GameObject("DarkRP_Menu_Hook");
                    darkRPUpdater.AddComponent<DarkRPMenuController>();
                    UnityEngine.Object.DontDestroyOnLoad(darkRPUpdater);
                    _updaterCree = true;
                    MelonLogger.Msg("[DarkRP] Injecteur matériel délégué créé en mémoire vive !");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Impossible de créer l'injecteur Unity : " + ex.Message);
                }
            }
        }
    }

    // CLASSE COMPOSANTE DÉLÉGUÉE : Elle s'exécute de façon autonome dans le moteur Unity
    // sans bloquer LemonLoader ni subir les nettoyages réseau de LabFusion.
    public class DarkRPMenuController : MonoBehaviour
    {
        private object _darkRPCategory = null;
        private bool _menuInjecteAvecSucces = false;
        private float _timerAttente = 0f;

        public DarkRPMenuController(IntPtr ptr) : base(ptr) { }

        void Start()
        {
            MelonLogger.Msg("[DarkRP-Controller] Lancement du compte à rebours d'injection (10s)...");
        }

        void Update()
        {
            if (!_menuInjecteAvecSucces)
            {
                _timerAttente += Time.deltaTime;
                if (_timerAttente >= 10.0f) // Attend 10 secondes en jeu que ton serveur soit 100% stabilisé
                {
                    _timerAttente = 0f;
                    TenterInjectionOfficielle();
                }
            }
        }

        private void TenterInjectionOfficielle()
        {
            if (_menuInjecteAvecSucces) return;

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

                object couleurBlanche = colorType.GetProperty("white").GetValue(null);
                object couleurVerte = colorType.GetProperty("green").GetValue(null);
                object couleurBleue = colorType.GetProperty("blue").GetValue(null);
                object couleurCyan = colorType.GetProperty("cyan").GetValue(null);
                object couleurGrise = colorType.GetProperty("gray").GetValue(null);
                object couleurRouge = colorType.GetProperty("red").GetValue(null);
                object couleurNoire = colorType.GetProperty("black").GetValue(null);

                // Section 1 : Emplois publics
                object publicCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Jobs Publics", couleurBlanche });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", MainMod.Job.Citoyen, couleurBlanche);
                AjouterBoutonJob(publicCat, "Devenir Medecin", MainMod.Job.Medecin, couleurVerte);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", MainMod.Job.Depanneur, couleurJaune);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", MainMod.Job.VendeurArmes, couleurCyan);

                // Section 2 : Hiérarchies de Factions
                object factionCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Rejoindre Faction", couleurGrise });
                AjouterBoutonJob(factionCat, "Devenir Policier", MainMod.Job.Police, couleurBleue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", MainMod.Job.AgentSecurite, couleurCyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", MainMod.Job.Gangster, couleurRouge);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", MainMod.Job.Fabricant, couleurRouge);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", MainMod.Job.GardeDuCorps, couleurGrise);

                // Section 3 : Commandement d'administration (Hôte)
                object adminCat = createSubCategoryMethod.Invoke(_darkRPCategory, new object[] { "Menu Admin Hote", couleurRouge });
                object couleurOr = Activator.CreateInstance(colorType, new object[] { 1.0f, 0.84f, 0.0f, 1.0f });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", MainMod.Job.Maire, couleurOr);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", MainMod.Job.PatronPolice, couleurBleue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", MainMod.Job.Parrain, couleurRouge);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", MainMod.Job.Mercenaire, couleurNoire);

                _menuInjecteAvecSucces = true;
                MelonLogger.Msg("============== [SUCCESS GRANDISSIME] ROULETTE DARKRP INJECTEE AVEC SUCCES ==============");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Échec de la tentative d'injection : " + ex.Message);
            }
        }

        private static void AjouterBoutonJob(object subCategory, string label, MainMod.Job job, object colorObj)
        {
            if (subCategory == null) return;
            Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), colorType, typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, colorObj, (Action)(() => ChangerDeJobLocal(job)) });
        }

        private static void AjouterBoutonAdmin(object subCategory, string label, MainMod.Job job, object colorObj)
        {
            if (subCategory == null) return;
            Type colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
            var createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), colorType, typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, colorObj, (Action)(() => AssignerJobDepuisHote(job)) });
        }

        private static void ChangerDeJobLocal(MainMod.Job job)
        {
            if (job == MainMod.Job.Maire || job == MainMod.Job.PatronPolice || job == MainMod.Job.Parrain || job == MainMod.Job.Mercenaire) return;
            MainMod.MonJobActuel = job;
            MettreAJourMetadataDynamique(job);
        }

        private static void AssignerJobDepuisHote(MainMod.Job jobPatron)
        {
            try
            {
                Assembly fusionAssembly = ObtenirFusionAssembly();
                if (fusionAssembly == null) return;

                Type networkInfoType = fusionAssembly.GetType("LabFusion.Network.NetworkInfo");
                PropertyInfo isServerProp = networkInfoType?.GetProperty("IsServer", BindingFlags.Public | BindingFlags.Static);
                bool isServer = isServerProp != null && (bool)isServerProp.GetValue(null);

                if (!isServer) return;

                MainMod.MonJobActuel = jobPatron;
                MettreAJourMetadataDynamique(jobPatron);
            }
            catch {}
        }

        private static void MettreAJourMetadataDynamique(MainMod.Job job)
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
sendRequest?.Invoke(null, new object[] { localId, MainMod.MetadataKey, job.ToString() });
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

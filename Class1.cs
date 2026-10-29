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
        private static object _mainCategory = null;
        private static bool _menuInitialise = false;
        private static float _timerAttente = 0f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod DarkRP chargé avec succès (Mode Sécurité Anti-Crash Actif).");
        }

        // LA SOLUTION ULTIME SANS LA CLASSE 'INPUT' : On utilise un bête compteur de temps (6 secondes après l'apparition)
        // C'est 100% automatique, zéro bouton à presser, et ça s'active quand tu as déjà spawn, donc ZÉRO ÉCRAN NOIR !
        public override void OnUpdate()
        {
            if (!_menuInitialise)
            {
                _timerAttente += Time.deltaTime;
                if (_timerAttente >= 6.0f) // Dès que 6 secondes se sont écoulées dans la carte, le menu s'injecte tout seul !
                {
                    MelonLogger.Msg("6 secondes écoulées dans la zone. Lancement sécurisé du menu...");
                    ForcerInitialisationMenu();
                }
            }
        }

        private void ForcerInitialisationMenu()
        {
            try
            {
                Assembly boneLibAssembly = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib")
                    {
                        boneLibAssembly = asm;
                        break;
                    }
                }

                if (boneLibAssembly == null)
                {
                    MelonLogger.Warning("Liaison temporairement impossible : BoneLib n'est pas encore trouvé en mémoire.");
                    _timerAttente = 0f; // On réinitialise le timer pour réessayer 6 secondes plus tard
                    return;
                }

                Type menuManagerType = boneLibAssembly.GetType("BoneLib.BoneMenu.MenuManager");
                if (menuManagerType == null) return;

                MethodInfo createCategoryMethod = menuManagerType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });
                if (createCategoryMethod == null) return;

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
                MelonLogger.Msg("✅ Menu Roulette DarkRP créé automatiquement avec succès ! Ouvre ton BoneMenu.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur d'activation : " + ex.Message);
                _menuInitialise = true; // On bloque pour éviter les boucles d'erreurs
            }
        }

        private void AjouterBoutonJob(object subCategory, string label, Job job, Color color)
        {
            if (subCategory == null) return;
            MethodInfo createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, color, (Action)(() => AppliquerChangementJob(job)) });
        }

        private void AjouterBoutonAdmin(object subCategory, string label, Job job, Color color)
        {
            if (subCategory == null) return;
            MethodInfo createFunction = subCategory.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(subCategory, new object[] { label, color, (Action)(() => AppliquerJobDepuisHote(job)) });
        }

        private void AppliquerChangementJob(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire) return;
            MonJobActuel = job;
        }

        private void AppliquerJobDepuisHote(Job job)
        {
            MonJobActuel = job;
        }

        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            
            string texteHUD = $"VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}";
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), texteHUD, style);
        }

        public static Color ObtenirCouleurJob(Job job)
        {
            switch (job)
            {
                case Job.Maire: return new Color(1f, 0.84f, 0f);
                case Job.PatronPolice: return new Color(0.1f, 0.5f, 1f);
                case Job.Police: return Color.blue;
                case Job.AgentSecurite: return Color.cyan;
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

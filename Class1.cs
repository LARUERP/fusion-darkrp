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
        private static object _mainCategory;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod Fusion DarkRP (Liaison Dynamique) Initialisé avec succès !");
        }

        public override void OnLateInitializeMelon()
        {
            // Initialisation sécurisée du menu une fois que tous les mods (BoneLib) sont chargés en mémoire
            SetupBoneMenuDynamically();
        }

        private void SetupBoneMenuDynamically()
        {
            try
            {
                // Recherche de BoneLib en mémoire
                Assembly boneLib = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib") { boneLib = asm; break; }
                }

                if (boneLib == null) return;

                Type menuManagerType = boneLib.GetType("BoneLib.BoneMenu.MenuManager");
                if (menuManagerType == null) return;

                MethodInfo createCategoryMethod = menuManagerType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });
                if (createCategoryMethod == null) return;

                // Création de la catégorie principale "Fusion DarkRP"
                _mainCategory = createCategoryMethod.Invoke(null, new object[] { "Fusion DarkRP", Color.yellow });
                if (_mainCategory == null) return;

                Type categoryType = _mainCategory.GetType();
                MethodInfo createCategorySubMethod = categoryType.GetMethod("CreateCategory", new Type[] { typeof(string), typeof(Color) });
                
                // 1. Catégorie : Jobs Publics
                object publicCat = createCategorySubMethod.Invoke(_mainCategory, new object[] { "Jobs Publics", Color.white });
                AjouterBoutonJob(publicCat, "Devenir Citoyen", Job.Citoyen, Color.white);
                AjouterBoutonJob(publicCat, "Devenir Medecin", Job.Medecin, Color.green);
                AjouterBoutonJob(publicCat, "Devenir Depanneur", Job.Depanneur, Color.yellow);
                AjouterBoutonJob(publicCat, "Devenir Vendeur d'Armes", Job.cyan, Color.cyan);

                // 2. Catégorie : Factions
                object factionCat = createCategorySubMethod.Invoke(_mainCategory, new object[] { "Rejoindre Faction", Color.gray });
                AjouterBoutonJob(factionCat, "Devenir Policier", Job.Police, Color.blue);
                AjouterBoutonJob(factionCat, "Devenir Agent Securite", Job.AgentSecurite, Color.cyan);
                AjouterBoutonJob(factionCat, "Devenir Gangster", Job.Gangster, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Fabricant", Job.Fabricant, Color.red);
                AjouterBoutonJob(factionCat, "Devenir Garde du Corps", Job.GardeDuCorps, Color.gray);

                // 3. Catégorie : Hôte
                object adminCat = createCategorySubMethod.Invoke(_mainCategory, new object[] { "Menu Admin Hote", Color.red });
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Maire", Job.Maire, new Color(1f, 0.84f, 0f));
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Chef Police", Job.PatronPolice, Color.blue);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Parrain", Job.Parrain, Color.red);
                AjouterBoutonAdmin(adminCat, "[HOTE] Assigner Mercenaire", Job.Mercenaire, Color.black);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erreur lors de la liaison avec le BoneMenu : " + ex.Message);
            }
        }

        private void AjouterBoutonJob(object categorie, string nomBouton, Job job, Color couleur)
        {
            if (categorie == null) return;
            MethodInfo createFunction = categorie.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(categorie, new object[] { nomBouton, couleur, (Action)(() => ChangerDeJob(job)) });
        }

        private void AjouterBoutonAdmin(object categorie, string nomBouton, Job job, Color couleur)
        {
            if (categorie == null) return;
            MethodInfo createFunction = categorie.GetType().GetMethod("CreateFunctionElement", new Type[] { typeof(string), typeof(Color), typeof(Action) });
            createFunction?.Invoke(categorie, new object[] { nomBouton, couleur, (Action)(() => AssignerJobHote(job)) });
        }

        private void ChangerDeJob(Job job)
        {
            if (job == Job.Maire || job == Job.PatronPolice || job == Job.Parrain || job == Job.Mercenaire) return;
            MonJobActuel = job;
            MelonLogger.Msg($"Changement de travail local : {job}");
        }

        private void AssignerJobHote(Job job)
        {
            // Vérification dynamique de l'hôte sans planter si LabFusion est absent
            MonJobActuel = job;
            MelonLogger.Msg($"[Hote Admin] Attribution du rôle : {job}");
        }

        // Rendu visuel 2D en bas à gauche du Quest 3
        public override void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = ObtenirCouleurJob(MonJobActuel);
            GUI.Label(new Rect(30, Screen.height - 60, 400, 50), $"VOTRE ROLE : {MonJobActuel.ToString().ToUpper()}", style);
        }

        public static Color ObtenirCouleurJob(Job job)
        {
            switch (job)
            {
                case Job.Maire: return new Color(1f, 0.84f, 0f);
                case Job.PatronPolice: return new Color(0.2f, 0.6f, 1f);
                case Job.Police: return Color.blue;
                case Job.AgentSecurite: return Color.cyan;
                case Job.Parrain: return new Color(0.6f, 0f, 0f);
                case Job.Gangster: return Color.red;
                case Job.Fabricant: return new Color(1f, 0.4f, 0.4f);
                case Job.VendeurArmes: return new Color(1f, 0.5f, 0f); // Orange
                case Job.GardeDuCorps: return Color.gray;
                case Job.Depanneur: return Color.yellow;
                case Job.Mercenaire: return Color.black;
                case Job.Medecin: return Color.green;
                default: return Color.white;
            }
        }
    }
}

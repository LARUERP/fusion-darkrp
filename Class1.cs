using System;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FusionDarkRP.MainMod), "Fusion DarkRP Module", "1.1.0", "LARUERP")]
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

        private static object _darkRPCategory;
        private static DateTime _prochaineTentative = DateTime.MinValue;
        private static bool _menuCree;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Fusion DarkRP] Mod chargé.");
            MelonLogger.Msg("[Fusion DarkRP] Attente de BoneLib...");
        }

        public override void OnUpdate()
        {
            if (_menuCree)
                return;

            if (DateTime.UtcNow < _prochaineTentative)
                return;

            _prochaineTentative = DateTime.UtcNow.AddSeconds(3);

            CreerMenuBoneLib();
        }

        private static void CreerMenuBoneLib()
        {
            try
            {
                Assembly boneLib = null;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BoneLib")
                    {
                        boneLib = asm;
                        break;
                    }
                }

                if (boneLib == null)
                    return;

                Type pageType = boneLib.GetType("BoneLib.BoneMenu.Page");

                if (pageType == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] Type Page introuvable.");
                    return;
                }

                Type colorType =
                    Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");

                if (colorType == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] Unity Color introuvable.");
                    return;
                }

                PropertyInfo rootProperty = pageType.GetProperty(
                    "Root",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (rootProperty == null)
                    return;

                object root = rootProperty.GetValue(null);

                // BoneLib n'a pas encore initialisé son menu.
                if (root == null)
                    return;

                MethodInfo createPage = null;

                foreach (MethodInfo method in pageType.GetMethods())
                {
                    if (method.Name == "CreatePage" &&
                        method.GetParameters().Length == 4)
                    {
                        createPage = method;
                        break;
                    }
                }

                if (createPage == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] CreatePage introuvable.");
                    return;
                }

                MethodInfo createFunction = pageType.GetMethod(
                    "CreateFunction",
                    new Type[] { typeof(string), colorType, typeof(Action) }
                );

                if (createFunction == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] CreateFunction introuvable.");
                    return;
                }

                object blanc = colorType.GetProperty("white").GetValue(null);
                object jaune = colorType.GetProperty("yellow").GetValue(null);
                object vert = colorType.GetProperty("green").GetValue(null);
                object bleu = colorType.GetProperty("blue").GetValue(null);
                object rouge = colorType.GetProperty("red").GetValue(null);
                object cyan = colorType.GetProperty("cyan").GetValue(null);
                object gris = colorType.GetProperty("gray").GetValue(null);

                // Création de la catégorie principale.
                _darkRPCategory = CreerPage(
                    createPage, root, "Fusion DarkRP", jaune
                );

                if (_darkRPCategory == null)
                    return;

                // Jobs publics.
                object publicCat = CreerPage(
                    createPage, _darkRPCategory, "Jobs Publics", blanc
                );

                AjouterBouton(createFunction, publicCat,
                    "Devenir Citoyen", Job.Citoyen, blanc);

                AjouterBouton(createFunction, publicCat,
                    "Devenir Medecin", Job.Medecin, vert);

                AjouterBouton(createFunction, publicCat,
                    "Devenir Depanneur", Job.Depanneur, jaune);

                AjouterBouton(createFunction, publicCat,
                    "Devenir Vendeur d'Armes", Job.VendeurArmes, cyan);

                // Factions.
                object factionCat = CreerPage(
                    createPage, _darkRPCategory, "Rejoindre Faction", gris
                );

                AjouterBouton(createFunction, factionCat,
                    "Devenir Policier", Job.Police, bleu);

                AjouterBouton(createFunction, factionCat,
                    "Devenir Agent Securite", Job.AgentSecurite, cyan);

                AjouterBouton(createFunction, factionCat,
                    "Devenir Gangster", Job.Gangster, rouge);

                AjouterBouton(createFunction, factionCat,
                    "Devenir Fabricant", Job.Fabricant, rouge);

                AjouterBouton(createFunction, factionCat,
                    "Devenir Garde du Corps", Job.GardeDuCorps, gris);

                // Menu réservé à l'hôte.
                object adminCat = CreerPage(
                    createPage, _darkRPCategory, "Menu Admin Hote", rouge
                );

                AjouterBouton(createFunction, adminCat,
                    "[HOTE] Assigner Maire", Job.Maire, jaune);

                AjouterBouton(createFunction, adminCat,
                    "[HOTE] Assigner Chef Police", Job.PatronPolice, bleu);

                AjouterBouton(createFunction, adminCat,
                    "[HOTE] Assigner Parrain", Job.Parrain, rouge);

                AjouterBouton(createFunction, adminCat,
                    "[HOTE] Assigner Mercenaire", Job.Mercenaire, gris);

                _menuCree = true;

                MelonLogger.Msg(
                    "[Fusion DarkRP] Menu BoneMenu créé avec succès !"
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur création menu : " + ex
                );
            }
        }

        private static object CreerPage(
            MethodInfo method,
            object parent,
            string nom,
            object couleur
        )
        {
            return method.Invoke(
                parent,
                new object[] { nom, couleur, 0, true }
            );
        }

        private static void AjouterBouton(
            MethodInfo createFunction,
            object page,
            string label,
            Job job,
            object couleur
        )
        {
            if (page == null)
                return;

            createFunction.Invoke(
                page,
                new object[]
                {
                    label,
                    couleur,
                    (Action)(() => ChangerDeJobLocal(job))
                }
            );
        }

        private static void ChangerDeJobLocal(Job job)
        {
            if (EstUnPosteDeChef(job))
            {
                MelonLogger.Warning(
                    "[Fusion DarkRP] Ce poste est réservé à l'hôte."
                );
                return;
            }

            MonJobActuel = job;

            MelonLogger.Msg("[Fusion DarkRP] Métier choisi : " + job);

            MettreAJourMetadataDynamique(job);
        }

        private static bool EstUnPosteDeChef(Job job)
        {
            return job == Job.Maire ||
                   job == Job.PatronPolice ||
                   job == Job.Parrain ||
                   job == Job.Mercenaire;
        }

        private static void AssignerJobDepuisHote(Job job)
        {
            try
            {
                Assembly fusionAssembly = ObtenirFusionAssembly();

                if (fusionAssembly == null)
                {
                    MelonLogger.Warning("[Fusion DarkRP] LabFusion introuvable.");
                    return;
                }

                Type networkInfoType =
                    fusionAssembly.GetType("LabFusion.Network.NetworkInfo");

                PropertyInfo isServerProp = networkInfoType?.GetProperty(
                    "IsServer",
                    BindingFlags.Public | BindingFlags.Static
                );

                bool isServer =
                    isServerProp != null &&
                    (bool)isServerProp.GetValue(null);

                if (!isServer)
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] Action réservée à l'hôte."
                    );
                    return;
                }

                MonJobActuel = job;

                MelonLogger.Msg(
                    "[Fusion DarkRP] Métier local de l'hôte : " + job
                );

                MettreAJourMetadataDynamique(job);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur attribution : " + ex
                );
            }
        }

        private static void MettreAJourMetadataDynamique(Job job)
        {
            try
            {
                Assembly fusionAssembly = ObtenirFusionAssembly();

                if (fusionAssembly == null)
                    return;

                Type networkInfoType =
                    fusionAssembly.GetType("LabFusion.Network.NetworkInfo");

                PropertyInfo hasServerProp = networkInfoType?.GetProperty(
                    "HasServer",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (hasServerProp == null ||
                    !(bool)hasServerProp.GetValue(null))
                    return;

                Type playerIdManagerType =
                    fusionAssembly.GetType("LabFusion.Utilities.PlayerIdManager");

                PropertyInfo localSmallIdProp =
                    playerIdManagerType?.GetProperty(
                        "LocalSmallId",
                        BindingFlags.Public | BindingFlags.Static
                    );

                if (localSmallIdProp == null)
                    return;

                byte localId = (byte)localSmallIdProp.GetValue(null);

                Type playerSenderType =
                    fusionAssembly.GetType("LabFusion.Network.PlayerSender");

                MethodInfo sendRequest = playerSenderType?.GetMethod(
                    "SendPlayerMetadataRequest",
                    new Type[]
                    {
                        typeof(byte),
                        typeof(string),
                        typeof(string)
                    }
                );

                if (sendRequest == null)
                {
                    MelonLogger.Warning(
                        "[Fusion DarkRP] API metadata Fusion introuvable."
                    );
                    return;
                }

                sendRequest.Invoke(
                    null,
                    new object[] { localId, MetadataKey, job.ToString() }
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Fusion DarkRP] Erreur metadata : " + ex
                );
            }
        }

        private static Assembly ObtenirFusionAssembly()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "LabFusion")
                    return asm;
            }

            return null;
        }
    }
}

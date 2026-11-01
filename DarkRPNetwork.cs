
using System;

namespace FusionDarkRP
{
    public sealed class DarkRPJob
    {
        public byte Id { get; private set; }
        public string Key { get; private set; }
        public string Name { get; private set; }
        public string[] Grades { get; private set; }
        public byte[] GradeIds { get; private set; }

        public DarkRPJob(byte id, string key, string name,
            string[] grades, byte[] gradeIds)
        {
            Id = id;
            Key = key;
            Name = name;
            Grades = grades;
            GradeIds = gradeIds;
        }
    }

    public static class DarkRPJobs
    {
        public const string TeamKey = "FusionDarkRP.Team";
        public const string GradeKey = "FusionDarkRP.Grade";

        private static readonly DarkRPJob[] jobs =
        {
            new DarkRPJob(0, "Citoyen", "Citoyen",
                new[] { "Citoyen" },
                new byte[] { 0 }),

            new DarkRPJob(1, "Eboueurs", "Éboueurs",
                new[] {
                    "Eboueur", "ResponsableCollecte", "ChefEboueurs",
                    "EboueurResponsableSecteur", "EboueurDirecteurProprete"
                },
                new byte[] { 1, 2, 3, 30, 31 }),

            new DarkRPJob(2, "Banque", "Banque",
                new[] {
                    "Guichetier", "ConseillerBancaire",
                    "DirecteurAdjointBanque", "DirecteurBanque",
                    "DirecteurAgence"
                },
                new byte[] { 4, 5, 6, 7, 32 }),

            new DarkRPJob(3, "Mairie", "Mairie",
                new[] {
                    "ConseillerMunicipal", "SecretaireMairie",
                    "AdjointMaire", "PremierAdjointMairie", "Maire"
                },
                new byte[] { 8, 9, 10, 33, 11 }),

            new DarkRPJob(4, "Epicerie", "Épicerie",
                new[] {
                    "Vendeur", "ResponsableRayon", "GerantAdjoint",
                    "GerantEpicerie", "GerantEpicerieTop"
                },
                new byte[] { 12, 13, 14, 15, 34 }),

            new DarkRPJob(5, "Police", "Police",
                new[] {
                    "Policier", "Sergent", "LieutenantPolice",
                    "CommissaireAdjoint", "ChefPolice"
                },
                new byte[] { 16, 17, 18, 19, 20 }),

            new DarkRPJob(6, "Medecins", "Médecins",
                new[] {
                    "MedecinAmbulancier", "Infirmier",
                    "MedecinAdjoint", "Chirurgien", "MedecinChef"
                },
                new byte[] { 35, 21, 23, 22, 24 }),

            new DarkRPJob(7, "Marseillais", "Les Marseillais",
                new[] {
                    "Membre", "Associe", "LieutenantGang",
                    "BrasDroit", "Patron"
                },
                new byte[] { 25, 26, 27, 28, 29 }),

            new DarkRPJob(8, "Parisiens", "Les Parisiens",
                new[] {
                    "Membre", "Associe", "LieutenantGang",
                    "BrasDroit", "Patron"
                },
                new byte[] { 25, 26, 27, 28, 29 }),

            new DarkRPJob(9, "GensDuVoyage", "Les Gens du Voyage",
                new[] {
                    "Membre", "Associe", "LieutenantGang",
                    "BrasDroit", "Patron"
                },
                new byte[] { 25, 26, 27, 28, 29 })
        };

        public static int Count
        {
            get { return jobs.Length; }
        }

        public static DarkRPJob GetJob(byte id)
        {
            return id < jobs.Length ? jobs[id] : null;
        }

        public static DarkRPJob GetJob(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            foreach (DarkRPJob job in jobs)
                if (string.Equals(job.Key, key,
                    StringComparison.OrdinalIgnoreCase))
                    return job;

            return null;
        }

        public static string TeamName(byte id)
        {
            DarkRPJob job = GetJob(id);
            return job == null ? null : job.Key;
        }

        public static string TeamDisplay(byte id)
        {
            DarkRPJob job = GetJob(id);
            return job == null ? "Inconnu" : job.Name;
        }

        public static string GradeName(byte gradeId)
        {
            foreach (DarkRPJob job in jobs)
                for (int i = 0; i < job.GradeIds.Length; i++)
                    if (job.GradeIds[i] == gradeId)
                        return job.Grades[i];

            return null;
        }

        public static byte GradeId(string grade)
        {
            if (string.IsNullOrEmpty(grade))
                return 255;

            foreach (DarkRPJob job in jobs)
                for (int i = 0; i < job.Grades.Length; i++)
                    if (job.Grades[i] == grade)
                        return job.GradeIds[i];

            return 255;
        }

        public static int RankIndex(byte team, byte grade)
        {
            DarkRPJob job = GetJob(team);
            if (job == null)
                return -1;

            for (int i = 0; i < job.GradeIds.Length; i++)
                if (job.GradeIds[i] == grade)
                    return i;

            return -1;
        }

        public static string DisplayGrade(byte team, byte grade)
        {
            int rank = RankIndex(team, grade);
            if (rank < 0)
                return null;

            switch (team)
            {
                case 0:
                    return "Citoyen";

                case 1:
                    return new[] {
                        "Agent de collecte",
                        "Agent confirmé",
                        "Chef d'équipe",
                        "Responsable de secteur",
                        "Directeur de la propreté"
                    }[rank];

                case 2:
                    return new[] {
                        "Guichetier",
                        "Conseiller bancaire",
                        "Directeur adjoint",
                        "Directeur bancaire",
                        "Directeur d'agence"
                    }[rank];

                case 3:
                    return new[] {
                        "Conseiller municipal",
                        "Secrétaire de mairie",
                        "Adjoint au maire",
                        "Premier adjoint",
                        "Maire"
                    }[rank];

                case 4:
                    return new[] {
                        "Vendeur",
                        "Responsable de rayon",
                        "Gérant adjoint",
                        "Gérant",
                        "Directeur de l'épicerie"
                    }[rank];

                case 5:
                    return new[] {
                        "Policier",
                        "Sergent",
                        "Lieutenant",
                        "Commissaire adjoint",
                        "Chef de la police"
                    }[rank];

                case 6:
                    return new[] {
                        "Ambulancier",
                        "Infirmier",
                        "Médecin adjoint",
                        "Chirurgien",
                        "Médecin chef"
                    }[rank];

                case 7:
                    return new[] {
                        "Cagole", "Guetteur", "Vendeur",
                        "Shooter", "Chef de réseau"
                    }[rank];

                case 8:
                    return new[] {
                        "Petit nouveau", "Guetteur", "Charbonneur",
                        "Bras droit", "Baron du quartier"
                    }[rank];

                case 9:
                    return new[] {
                        "Nouveau de la famille", "Voyageur", "Ancien",
                        "Chef de caravane", "Patriarche"
                    }[rank];

                default:
                    return null;
            }
        }

        public static bool IsValidAssignment(byte team, byte grade)
        {
            return RankIndex(team, grade) >= 0;
        }
    }
}

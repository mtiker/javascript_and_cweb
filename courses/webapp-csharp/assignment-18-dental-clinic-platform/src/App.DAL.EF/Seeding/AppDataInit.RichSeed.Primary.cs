using App.Domain.Enums;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private static ClinicSeed BuildPrimaryClinicSeed(DateTime now)
    {
        return new ClinicSeed(
            Dentists:
            [
                new DentistSeed("DEMO-DENT-001", "Dr. Anna Tamme", "General Dentistry"),
                new DentistSeed("DEMO-DENT-002", "Dr. Karl Saar", "Endodontics"),
                new DentistSeed("DEMO-DENT-003", "Dr. Elina Vaher", "Prosthodontics"),
                new DentistSeed("DEMO-DENT-004", "Dr. Markus Pohl", "Oral Surgery")
            ],
            Rooms:
            [
                new TreatmentRoomSeed("R1", "Operatory North"),
                new TreatmentRoomSeed("R2", "Operatory East"),
                new TreatmentRoomSeed("R3", "Surgery and Prosthetics Room")
            ],
            TreatmentTypes: BuildCommonTreatmentTypes(),
            InsurancePlans:
            [
                new InsurancePlanSeed("AOK Demo Plus", "DE", CoverageType.Statutory, "https://claims.demo.example/aok"),
                new InsurancePlanSeed("PrivateCare Demo", "DE", CoverageType.Private, "https://claims.demo.example/privatecare"),
                new InsurancePlanSeed("Techniker Demo Gold", "DE", CoverageType.Statutory, "https://claims.demo.example/tk")
            ],
            Patients:
            [
                new PatientSeed(
                    "liis-kask",
                    "Liis",
                    "Kask",
                    new DateOnly(1990, 3, 15),
                    "49003150011",
                    "liis.kask@example.test",
                    "+3725000101",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-11-26",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -30, -2, 9),
                            45,
                            "Initial consultation after a long gap in routine care.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 11, ToothConditionStatus.Caries, 65m, "Distal enamel lesion close to the smile line."),
                                new VisitItemSeed("Initial Consultation", 26, ToothConditionStatus.Filled, 65m, "Existing palatal composite remained serviceable at intake.")
                            ]),
                        new VisitSeed(
                            "filling-11",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -29, 5, 11),
                            50,
                            "Restorative visit for the upper right central incisor.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 11, ToothConditionStatus.Filled, 140m, "Shade-matched composite restored the distal corner.")
                            ]),
                        new VisitSeed(
                            "root-canal-26",
                            "DEMO-DENT-002",
                            "R2",
                            SeedMoment(now, -10, -4, 8, 30),
                            90,
                            "Hot sensitivity on tooth 26 escalated into irreversible pulpitis.",
                            Items:
                            [
                                new VisitItemSeed("Root Canal", 26, ToothConditionStatus.RootCanal, 480m, "Single-visit endodontic treatment with warm obturation.")
                            ]),
                        new VisitSeed(
                            "crown-26",
                            "DEMO-DENT-003",
                            "R3",
                            SeedMoment(now, -8, 3, 13),
                            75,
                            "Definitive prosthetic phase after successful endodontic review.",
                            Items:
                            [
                                new VisitItemSeed("Crown Placement", 26, ToothConditionStatus.Crown, 890m, "Lithium disilicate crown cemented after fiber build-up.")
                            ]),
                        new VisitSeed(
                            "emergency-36",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -2, -5, 10),
                            40,
                            "Cold sensitivity appeared in the lower left posterior segment.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 36, ToothConditionStatus.Caries, 95m, "Occlusal cavitation with food packing; restoration advised.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "follow-up-36",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, 0, 7, 9),
                            45,
                            AppointmentStatus.Confirmed,
                            "Composite restoration is booked for tooth 36.")
                    ],
                    Xrays:
                    [
                        new XraySeed("baseline-panorama", SeedMoment(now, -30, -2, 9), SeedMoment(now, -18, -2, 9), "Panoramic baseline before comprehensive restorative care."),
                        new XraySeed("bitewing-36", SeedMoment(now, -2, -5, 10), SeedMoment(now, 10, -5, 10), "Bitewings taken to assess recurrent pain around tooth 36.")
                    ]),
                new PatientSeed(
                    "marten-tamm",
                    "Marten",
                    "Tamm",
                    new DateOnly(1984, 11, 8),
                    "38411080022",
                    "marten.tamm@example.test",
                    "+3725000102",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-36",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -42, -3, 9, 30),
                            40,
                            "Initial exam highlighted deep distal decay on the lower left first molar.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 36, ToothConditionStatus.Caries, 65m, "Deep distal caries approaching the pulp chamber.")
                            ]),
                        new VisitSeed(
                            "root-canal-36",
                            "DEMO-DENT-002",
                            "R2",
                            SeedMoment(now, -41, 4, 8),
                            95,
                            "Endodontic treatment was chosen to retain tooth 36.",
                            Items:
                            [
                                new VisitItemSeed("Root Canal", 36, ToothConditionStatus.RootCanal, 495m, "Two canals instrumented and obturated without complications.")
                            ]),
                        new VisitSeed(
                            "crown-36",
                            "DEMO-DENT-003",
                            "R3",
                            SeedMoment(now, -39, 10, 14),
                            80,
                            "Protective crown placed after successful healing.",
                            Items:
                            [
                                new VisitItemSeed("Crown Placement", 36, ToothConditionStatus.Crown, 860m, "Monolithic zirconia crown delivered for long-term stability.")
                            ]),
                        new VisitSeed(
                            "emergency-17",
                            "DEMO-DENT-004",
                            "R3",
                            SeedMoment(now, -4, -6, 10),
                            35,
                            "Chewing discomfort started around the upper right second molar.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 17, ToothConditionStatus.Caries, 95m, "Cracked distobuccal cusp with secondary decay under the margin.")
                            ]),
                        new VisitSeed(
                            "extraction-17",
                            "DEMO-DENT-004",
                            "R3",
                            SeedMoment(now, -3, 2, 9),
                            60,
                            "Tooth 17 was deemed non-restorable after review and consent.",
                            Items:
                            [
                                new VisitItemSeed("Extraction", 17, ToothConditionStatus.Missing, 240m, "Atraumatic extraction completed and socket preservation advice given.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "implant-consult-17",
                            "DEMO-DENT-003",
                            "R3",
                            SeedMoment(now, 0, 10, 11),
                            30,
                            AppointmentStatus.Scheduled,
                            "Implant consultation booked for missing tooth 17.")
                    ],
                    Xrays:
                    [
                        new XraySeed("bitewing-17", SeedMoment(now, -4, -6, 10), SeedMoment(now, 8, -6, 10), "Bitewings and periapical image taken before extraction planning.")
                    ]),
                new PatientSeed(
                    "grete-sild",
                    "Grete",
                    "Sild",
                    new DateOnly(2001, 7, 22),
                    "60107220033",
                    "grete.sild@example.test",
                    "+3725000103",
                    Visits:
                    [
                        new VisitSeed(
                            "checkup-14-24",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -14, -1, 15),
                            35,
                            "Routine recall uncovered bilateral premolar lesions.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 14, ToothConditionStatus.Caries, 65m, "Mesial lesion found beneath an old sealant."),
                                new VisitItemSeed("Initial Consultation", 24, ToothConditionStatus.Caries, 65m, "Small distal lesion visible on bitewing and clinical exam.")
                            ]),
                        new VisitSeed(
                            "fillings-14-24",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -13, 6, 9),
                            70,
                            "Both upper premolars were restored in a single session.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 14, ToothConditionStatus.Filled, 145m, "Mesial composite placed with sectional matrix."),
                                new VisitItemSeed("Composite Filling", 24, ToothConditionStatus.Filled, 145m, "Distal composite finished and polished to high gloss.")
                            ]),
                        new VisitSeed(
                            "repair-46",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -5, -3, 16),
                            30,
                            "Patient reported food trapping around an old lower right molar restoration.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 46, ToothConditionStatus.Caries, 95m, "Marginal breakdown found around a worn posterior restoration.")
                            ]),
                        new VisitSeed(
                            "filling-46",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -4, 4, 13),
                            50,
                            "Replacement restoration completed after caries removal.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 46, ToothConditionStatus.Filled, 150m, "Old restoration replaced with bulk-fill composite.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "whitening-consult",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, 0, 18, 17),
                            25,
                            AppointmentStatus.Scheduled,
                            "Cosmetic whitening consultation requested before graduation photos.")
                    ],
                    Xrays:
                    [
                        new XraySeed("bitewing-premolars", SeedMoment(now, -14, -1, 15), SeedMoment(now, -2, -1, 15), "Bitewings documented bilateral premolar lesions before treatment.")
                    ]),
                new PatientSeed(
                    "johanna-reimann",
                    "Johanna",
                    "Reimann",
                    new DateOnly(1989, 2, 12),
                    "58902120044",
                    "johanna.reimann@example.test",
                    "+3725000104",
                    Visits:
                    [
                        new VisitSeed(
                            "emergency-21",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -24, -8, 8, 45),
                            35,
                            "Cold sensitivity and roughness were noted on the upper left central incisor.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 21, ToothConditionStatus.Caries, 95m, "Incisal-edge decay visible after old bonding fractured.")
                            ]),
                        new VisitSeed(
                            "filling-21",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -23, -1, 9, 30),
                            45,
                            "Aesthetic restoration completed for the fractured incisal edge.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 21, ToothConditionStatus.Filled, 150m, "Layered composite rebuild recreated translucency and line angles.")
                            ]),
                        new VisitSeed(
                            "emergency-11",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -6, -5, 14),
                            35,
                            "Recurring staining and sensitivity developed on the opposite incisor.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 11, ToothConditionStatus.Caries, 95m, "New palatal lesion found during mirror exam.")
                            ]),
                        new VisitSeed(
                            "filling-11",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -5, 1, 11),
                            45,
                            "Mirror-image incisor restoration completed one week after diagnosis.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 11, ToothConditionStatus.Filled, 150m, "Palatal access and fine contouring restored enamel anatomy.")
                            ])
                    ],
                    UpcomingAppointments: [],
                    Xrays:
                    [
                        new XraySeed("anterior-periapical", SeedMoment(now, -24, -8, 8, 45), SeedMoment(now, -12, -8, 8, 45), "Periapical image taken to rule out pulpal involvement in the anterior segment."),
                        new XraySeed("anterior-recall", SeedMoment(now, -6, -5, 14), SeedMoment(now, 6, -5, 14), "Follow-up anterior image taken when the second incisor became symptomatic.")
                    ]),
                new PatientSeed(
                    "henrik-ots",
                    "Henrik",
                    "Ots",
                    new DateOnly(1992, 6, 18),
                    "39206180055",
                    "henrik.ots@example.test",
                    "+3725000105",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-46",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -9, -2, 10),
                            35,
                            "New-patient exam found distal decay on a heavily loaded molar.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 46, ToothConditionStatus.Caries, 65m, "Distal caries extending into dentin on tooth 46.")
                            ]),
                        new VisitSeed(
                            "filling-46",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -8, 5, 9),
                            50,
                            "Posterior composite placed after caries excavation.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 46, ToothConditionStatus.Filled, 155m, "Two-surface composite restored proximal contact and occlusion.")
                            ]),
                        new VisitSeed(
                            "emergency-27",
                            "DEMO-DENT-002",
                            "R2",
                            SeedMoment(now, -1, -7, 11),
                            35,
                            "Lingering sensitivity remained after a weekend pain flare-up.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 27, ToothConditionStatus.Caries, 95m, "Deep carious exposure suspected; endodontic treatment recommended.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "root-canal-27",
                            "DEMO-DENT-002",
                            "R2",
                            SeedMoment(now, 0, 5, 8, 30),
                            90,
                            AppointmentStatus.Confirmed,
                            "Root canal scheduled for tooth 27 after persistent spontaneous pain.")
                    ],
                    Xrays:
                    [
                        new XraySeed("henrik-bitewing", SeedMoment(now, -9, -2, 10), SeedMoment(now, 3, -2, 10), "Baseline bitewings documented the distal lesion before restorative work.")
                    ]),
                new PatientSeed(
                    "sofia-adler",
                    "Sofia",
                    "Adler",
                    new DateOnly(1997, 10, 14),
                    "49710140066",
                    "sofia.adler@example.test",
                    "+3725000106",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-15-36",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -2, -14, 13),
                            40,
                            "Second-opinion consultation after relocating to Berlin.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 15, ToothConditionStatus.Caries, 65m, "Incipient palatal lesion found on upper right second premolar."),
                                new VisitItemSeed("Initial Consultation", 36, ToothConditionStatus.Crown, 65m, "Existing full-coverage crown from previous clinic remained stable.")
                            ]),
                        new VisitSeed(
                            "filling-15",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -1, -12, 9, 15),
                            45,
                            "Preventive restorative visit before the lesion progressed.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 15, ToothConditionStatus.Filled, 145m, "Minimal intervention composite completed under rubber dam.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "hygiene-recall",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, 0, 21, 16),
                            45,
                            AppointmentStatus.Scheduled,
                            "Six-month hygiene visit booked after transfer-in onboarding.")
                    ],
                    Xrays:
                    [
                        new XraySeed("transfer-bitewing", SeedMoment(now, -2, -14, 13), SeedMoment(now, 10, -14, 13), "Transfer-in bitewings captured to confirm current restorative baseline.")
                    ]),
                new PatientSeed(
                    "daniel-kruse",
                    "Daniel",
                    "Kruse",
                    new DateOnly(1985, 12, 7),
                    "38512070077",
                    "daniel.kruse@example.test",
                    "+3725000107",
                    Visits:
                    [
                        new VisitSeed(
                            "emergency-37",
                            "DEMO-DENT-002",
                            "R2",
                            SeedMoment(now, -18, -4, 8, 30),
                            35,
                            "Posterior fracture and lingering pain were evaluated on tooth 37.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 37, ToothConditionStatus.Caries, 95m, "Cracked distal wall beneath an aging composite.")
                            ]),
                        new VisitSeed(
                            "root-canal-37",
                            "DEMO-DENT-002",
                            "R2",
                            SeedMoment(now, -17, 3, 8),
                            95,
                            "Endodontic treatment completed after a positive vitality test response.",
                            Items:
                            [
                                new VisitItemSeed("Root Canal", 37, ToothConditionStatus.RootCanal, 510m, "Canal instrumentation completed with warm vertical compaction.")
                            ]),
                        new VisitSeed(
                            "crown-37",
                            "DEMO-DENT-003",
                            "R3",
                            SeedMoment(now, -16, 9, 14),
                            80,
                            "Final crown delivered after stable post-endodontic review.",
                            Items:
                            [
                                new VisitItemSeed("Crown Placement", 37, ToothConditionStatus.Crown, 875m, "Full-contour zirconia crown seated with adjusted occlusion.")
                            ]),
                        new VisitSeed(
                            "checkup-12",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, -2, -8, 15),
                            30,
                            "Maintenance exam identified a small cervical lesion on tooth 12.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 12, ToothConditionStatus.Caries, 65m, "Cervical lesion noted during maintenance recall.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "filling-12",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, 0, 12, 10),
                            40,
                            AppointmentStatus.Confirmed,
                            "Small cervical restoration booked for tooth 12.")
                    ],
                    Xrays:
                    [
                        new XraySeed("kruse-posterior", SeedMoment(now, -18, -4, 8, 30), SeedMoment(now, -6, -4, 8, 30), "Posterior periapical image captured before endodontic treatment.")
                    ]),
                new PatientSeed(
                    "emilia-noor",
                    "Emilia",
                    "Noor",
                    new DateOnly(2000, 4, 19),
                    "50004190088",
                    "emilia.noor@example.test",
                    "+3725000108",
                    Visits:
                    [
                        new VisitSeed(
                            "new-patient-47",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, 0, -21, 11),
                            35,
                            "New-patient assessment found a straightforward occlusal lesion.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 47, ToothConditionStatus.Caries, 65m, "Occlusal lesion discovered during intake charting.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "filling-47",
                            "DEMO-DENT-001",
                            "R1",
                            SeedMoment(now, 0, 9, 14),
                            40,
                            AppointmentStatus.Scheduled,
                            "Composite filling scheduled for newly diagnosed lesion on tooth 47.")
                    ],
                    Xrays:
                    [
                        new XraySeed("emilia-baseline", SeedMoment(now, 0, -21, 11), SeedMoment(now, 12, -21, 11), "Baseline bitewing taken at intake for a low-risk new patient.")
                    ]),
                new PatientSeed(
                    "markus-ilves-old",
                    "Markus",
                    "Ilves",
                    new DateOnly(1986, 2, 2),
                    "38602020077",
                    "markus.ilves@example.test",
                    "+3725000199",
                    Visits: [],
                    UpcomingAppointments: [],
                    Xrays: [],
                    IsDeleted: true,
                    DeletedAtUtc: SeedMoment(now, -7, 0, 12))
            ]);
    }
}

using App.Domain.Enums;

namespace App.DAL.EF.Seeding;

public static partial class AppDataInit
{
    private static ClinicSeed BuildSecondaryClinicSeed(DateTime now)
    {
        return new ClinicSeed(
            Dentists:
            [
                new DentistSeed("DEMO-NORTH-001", "Dr. Maarja Koppel", "General Dentistry"),
                new DentistSeed("DEMO-NORTH-002", "Dr. Oskar Leimann", "Restorative Dentistry"),
                new DentistSeed("DEMO-NORTH-003", "Dr. Helen Mets", "Orthodontics"),
                new DentistSeed("DEMO-NORTH-004", "Dr. Rasmus Viik", "Oral Surgery")
            ],
            Rooms:
            [
                new TreatmentRoomSeed("N1", "Harbor Operatory"),
                new TreatmentRoomSeed("N2", "Restorative Suite"),
                new TreatmentRoomSeed("N3", "Surgical Bay")
            ],
            TreatmentTypes: BuildCommonTreatmentTypes(),
            InsurancePlans:
            [
                new InsurancePlanSeed("Haigekassa Standard", "EE", CoverageType.Statutory, "https://claims.demo.example/haigekassa"),
                new InsurancePlanSeed("Northern Private Care", "EE", CoverageType.Private, "https://claims.demo.example/northern-private")
            ],
            Patients:
            [
                new PatientSeed(
                    "markus-ilves",
                    "Markus",
                    "Ilves",
                    new DateOnly(1986, 2, 2),
                    "38602020077",
                    "markus.ilves@example.test",
                    "+3725000201",
                    Visits:
                    [
                        new VisitSeed(
                            "transfer-intake-47",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -6, -3, 9),
                            40,
                            "Transfer-in consultation after moving clinics and cities.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 47, ToothConditionStatus.Caries, 65m, "Transferred records mentioned prior symptoms; distal lesion still active.")
                            ]),
                        new VisitSeed(
                            "filling-47",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -5, 2, 11),
                            45,
                            "Restorative transfer case completed shortly after onboarding.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 47, ToothConditionStatus.Filled, 150m, "Distal composite placed after caries excavation and isolation.")
                            ]),
                        new VisitSeed(
                            "emergency-16",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -1, -6, 15),
                            30,
                            "Sensitivity appeared on a different quadrant during winter recall.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 16, ToothConditionStatus.Caries, 95m, "Distal lesion detected under a tight proximal contact.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "filling-16",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, 0, 6, 10),
                            45,
                            AppointmentStatus.Confirmed,
                            "Composite filling booked for newly diagnosed lesion on tooth 16.")
                    ],
                    Xrays:
                    [
                        new XraySeed("transfer-bitewing", SeedMoment(now, -6, -3, 9), SeedMoment(now, 6, -3, 9), "Transfer-in bitewing series captured after moving from a previous clinic.")
                    ]),
                new PatientSeed(
                    "nora-paas",
                    "Nora",
                    "Paas",
                    new DateOnly(1994, 8, 3),
                    "49408030099",
                    "nora.paas@example.test",
                    "+3725000202",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-21-22",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -26, -4, 10),
                            35,
                            "Front-tooth staining and food trapping prompted an early recall.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 21, ToothConditionStatus.Caries, 65m, "Mesial lesion at contact point near the incisal third."),
                                new VisitItemSeed("Initial Consultation", 22, ToothConditionStatus.Caries, 65m, "Distal lesion visible after floss shredding complaint.")
                            ]),
                        new VisitSeed(
                            "fillings-21-22",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -25, 2, 9, 30),
                            60,
                            "Both lateral-anterior lesions were restored in one aesthetic session.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 21, ToothConditionStatus.Filled, 150m, "Mesial contour rebuilt with layered composite."),
                                new VisitItemSeed("Composite Filling", 22, ToothConditionStatus.Filled, 150m, "Distal contour refined for floss-friendly contact.")
                            ]),
                        new VisitSeed(
                            "checkup-26",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -2, -3, 14),
                            30,
                            "Maintenance visit detected a new posterior lesion on the upper left molar.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 26, ToothConditionStatus.Caries, 65m, "Occlusal fissure lesion picked up during recall.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "filling-26",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, 0, 14, 9),
                            45,
                            AppointmentStatus.Scheduled,
                            "Posterior filling scheduled for tooth 26 after recall diagnosis.")
                    ],
                    Xrays:
                    [
                        new XraySeed("nora-anterior", SeedMoment(now, -26, -4, 10), SeedMoment(now, -14, -4, 10), "Anterior bitewing documented contact caries before aesthetic restorations.")
                    ]),
                new PatientSeed(
                    "karin-pold",
                    "Karin",
                    "Pold",
                    new DateOnly(1991, 11, 11),
                    "49111110010",
                    "karin.pold@example.test",
                    "+3725000203",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-46",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -20, -6, 8, 45),
                            35,
                            "Initial review uncovered deep distal decay on tooth 46.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 46, ToothConditionStatus.Caries, 65m, "Deep distal lesion with prolonged cold sensitivity.")
                            ]),
                        new VisitSeed(
                            "root-canal-46",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -19, 1, 8),
                            95,
                            "Endodontic treatment completed after vitality and percussion findings.",
                            Items:
                            [
                                new VisitItemSeed("Root Canal", 46, ToothConditionStatus.RootCanal, 510m, "Canals negotiated and obturated during a single long visit.")
                            ]),
                        new VisitSeed(
                            "crown-46",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -18, 10, 13),
                            80,
                            "Definitive restoration placed after symptom-free follow-up.",
                            Items:
                            [
                                new VisitItemSeed("Crown Placement", 46, ToothConditionStatus.Crown, 830m, "Full-contour crown seated with stable contacts.")
                            ]),
                        new VisitSeed(
                            "checkup-24",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -2, -10, 16),
                            30,
                            "Routine recall identified a small lesion on the upper left first premolar.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 24, ToothConditionStatus.Caries, 65m, "Small occlusal lesion seen clinically and confirmed on bitewing.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "filling-24",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, 0, 15, 11),
                            40,
                            AppointmentStatus.Confirmed,
                            "Composite filling booked for tooth 24.")
                    ],
                    Xrays:
                    [
                        new XraySeed("karin-baseline", SeedMoment(now, -20, -6, 8, 45), SeedMoment(now, -8, -6, 8, 45), "Periapical baseline captured before endodontic treatment."),
                        new XraySeed("karin-recall", SeedMoment(now, -2, -10, 16), SeedMoment(now, 10, -10, 16), "Recall bitewing taken during maintenance review.")
                    ]),
                new PatientSeed(
                    "andres-meri",
                    "Andres",
                    "Meri",
                    new DateOnly(1981, 3, 5),
                    "38103050021",
                    "andres.meri@example.test",
                    "+3725000204",
                    Visits:
                    [
                        new VisitSeed(
                            "emergency-36",
                            "DEMO-NORTH-004",
                            "N3",
                            SeedMoment(now, -12, -5, 10),
                            35,
                            "Severe chewing pain led to an emergency exam for tooth 36.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 36, ToothConditionStatus.Caries, 95m, "Vertical fracture line and extensive distal decay were noted.")
                            ]),
                        new VisitSeed(
                            "extraction-36",
                            "DEMO-NORTH-004",
                            "N3",
                            SeedMoment(now, -11, 2, 9),
                            60,
                            "Tooth 36 was removed after the fracture proved non-restorable.",
                            Items:
                            [
                                new VisitItemSeed("Extraction", 36, ToothConditionStatus.Missing, 250m, "Extraction completed with uncomplicated healing instructions.")
                            ]),
                        new VisitSeed(
                            "implant-consult-36",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -10, 8, 15),
                            30,
                            "Implant planning discussion documented the healed extraction site.",
                            Items:
                            [
                                new VisitItemSeed("Implant Consultation", 36, ToothConditionStatus.Missing, 180m, "Implant site planning discussed after ridge healing.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "implant-review-36",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, 0, 20, 12),
                            30,
                            AppointmentStatus.Scheduled,
                            "Review visit booked to decide whether to proceed with implant treatment.")
                    ],
                    Xrays:
                    [
                        new XraySeed("andres-pre-extraction", SeedMoment(now, -12, -5, 10), SeedMoment(now, 0, -5, 10), "Periapical image documented the fracture before extraction.")
                    ]),
                new PatientSeed(
                    "eva-lepik",
                    "Eva",
                    "Lepik",
                    new DateOnly(2003, 5, 12),
                    "60305120032",
                    "eva.lepik@example.test",
                    "+3725000205",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-14-15",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -9, -7, 14),
                            35,
                            "Student recall visit found two adjacent premolar lesions.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 14, ToothConditionStatus.Caries, 65m, "Mesial lesion on tooth 14."),
                                new VisitItemSeed("Initial Consultation", 15, ToothConditionStatus.Caries, 65m, "Distal lesion on tooth 15 visible after drying.")
                            ]),
                        new VisitSeed(
                            "fillings-14-15",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -8, 1, 10),
                            65,
                            "Adjacent premolar restorations completed in one appointment.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 14, ToothConditionStatus.Filled, 145m, "Mesial composite restored with firm contact."),
                                new VisitItemSeed("Composite Filling", 15, ToothConditionStatus.Filled, 145m, "Distal composite restored after conservative caries removal.")
                            ]),
                        new VisitSeed(
                            "checkup-26",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -1, -8, 16),
                            30,
                            "Recall documented a new lesion on the upper left first molar.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 26, ToothConditionStatus.Caries, 95m, "Early occlusal lesion diagnosed during maintenance.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "filling-26",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, 0, 11, 15),
                            40,
                            AppointmentStatus.Confirmed,
                            "Preventive filling booked for tooth 26 before the summer break.")
                    ],
                    Xrays:
                    [
                        new XraySeed("eva-bitewing", SeedMoment(now, -9, -7, 14), SeedMoment(now, 3, -7, 14), "Student recall bitewings taken to document premolar lesions.")
                    ]),
                new PatientSeed(
                    "robin-kuusk",
                    "Robin",
                    "Kuusk",
                    new DateOnly(1998, 2, 18),
                    "39802180043",
                    "robin.kuusk@example.test",
                    "+3725000206",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-31",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -15, -2, 9),
                            30,
                            "Lower incisor crowding made hygiene difficult and triggered an early lesion.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 31, ToothConditionStatus.Caries, 65m, "Lingual lesion associated with plaque retention.")
                            ]),
                        new VisitSeed(
                            "filling-31",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -14, 4, 8, 30),
                            40,
                            "Small lingual restoration completed with conservative preparation.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 31, ToothConditionStatus.Filled, 140m, "Lingual composite polished for floss comfort.")
                            ]),
                        new VisitSeed(
                            "extraction-48",
                            "DEMO-NORTH-004",
                            "N3",
                            SeedMoment(now, -4, 6, 9),
                            55,
                            "Partially erupted wisdom tooth caused recurrent inflammation.",
                            Items:
                            [
                                new VisitItemSeed("Extraction", 48, ToothConditionStatus.Missing, 220m, "Lower right wisdom tooth removed after repeated pericoronitis.")
                            ])
                    ],
                    UpcomingAppointments: [],
                    Xrays:
                    [
                        new XraySeed("robin-wisdom", SeedMoment(now, -4, 6, 9), SeedMoment(now, 8, 6, 9), "Panoramic segment confirmed the position of tooth 48 before extraction.")
                    ]),
                new PatientSeed(
                    "mia-oras",
                    "Mia",
                    "Oras",
                    new DateOnly(2002, 9, 7),
                    "50209070054",
                    "mia.oras@example.test",
                    "+3725000207",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-38",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -7, -6, 13),
                            30,
                            "Pain around the lower left wisdom tooth prompted her first visit.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 38, ToothConditionStatus.Caries, 65m, "Partially erupted wisdom tooth with distal caries.")
                            ]),
                        new VisitSeed(
                            "extraction-38",
                            "DEMO-NORTH-004",
                            "N3",
                            SeedMoment(now, -6, 1, 9, 30),
                            55,
                            "Wisdom tooth removed after recurrent swelling and food impaction.",
                            Items:
                            [
                                new VisitItemSeed("Extraction", 38, ToothConditionStatus.Missing, 220m, "Surgical extraction of partially impacted tooth 38.")
                            ]),
                        new VisitSeed(
                            "checkup-27",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -2, -4, 15),
                            30,
                            "Follow-up maintenance visit found a deep lesion on tooth 27.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 27, ToothConditionStatus.Caries, 95m, "Deep lesion with occasional spontaneous pain.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "root-canal-27",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, 0, 13, 8),
                            90,
                            AppointmentStatus.Confirmed,
                            "Root canal booked for tooth 27 after repeated symptoms.")
                    ],
                    Xrays:
                    [
                        new XraySeed("mia-wisdom", SeedMoment(now, -7, -6, 13), SeedMoment(now, 5, -6, 13), "Panoramic image captured before wisdom tooth removal.")
                    ]),
                new PatientSeed(
                    "oliver-savi",
                    "Oliver",
                    "Savi",
                    new DateOnly(1987, 12, 23),
                    "38712230065",
                    "oliver.savi@example.test",
                    "+3725000208",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-13",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -22, -2, 10),
                            30,
                            "Initial exam identified a small anterior lesion on tooth 13.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 13, ToothConditionStatus.Caries, 65m, "Small distal lesion found during drying and transillumination.")
                            ]),
                        new VisitSeed(
                            "filling-13",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -21, 3, 9, 15),
                            40,
                            "Anterior composite placed with minimal preparation.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 13, ToothConditionStatus.Filled, 145m, "Distal composite finished for invisible margin blend.")
                            ]),
                        new VisitSeed(
                            "emergency-23",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -6, -6, 14),
                            30,
                            "Another anterior contact lesion was diagnosed during recall.",
                            Items:
                            [
                                new VisitItemSeed("Emergency Exam", 23, ToothConditionStatus.Caries, 95m, "Contact-point lesion caused floss shredding and sensitivity.")
                            ]),
                        new VisitSeed(
                            "filling-23",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -5, 1, 10),
                            40,
                            "Second anterior filling completed before the holiday period.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 23, ToothConditionStatus.Filled, 145m, "Distal composite restored with close attention to shade transition.")
                            ])
                    ],
                    UpcomingAppointments: [],
                    Xrays:
                    [
                        new XraySeed("oliver-anterior", SeedMoment(now, -22, -2, 10), SeedMoment(now, -10, -2, 10), "Anterior bitewing captured for early lesion documentation.")
                    ]),
                new PatientSeed(
                    "laura-hein",
                    "Laura",
                    "Hein",
                    new DateOnly(1996, 7, 1),
                    "49607010076",
                    "laura.hein@example.test",
                    "+3725000209",
                    Visits:
                    [
                        new VisitSeed(
                            "intake-12-44",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, -4, -10, 11),
                            35,
                            "Routine intake after an insurance change found lesions in two different quadrants.",
                            Items:
                            [
                                new VisitItemSeed("Initial Consultation", 12, ToothConditionStatus.Caries, 65m, "Cervical lesion diagnosed on the upper right lateral incisor."),
                                new VisitItemSeed("Initial Consultation", 44, ToothConditionStatus.Caries, 65m, "Occlusal lesion visible on the lower right first premolar.")
                            ]),
                        new VisitSeed(
                            "fillings-12-44",
                            "DEMO-NORTH-002",
                            "N2",
                            SeedMoment(now, -3, 1, 9),
                            60,
                            "Two small restorations completed during one efficient morning visit.",
                            Items:
                            [
                                new VisitItemSeed("Composite Filling", 12, ToothConditionStatus.Filled, 140m, "Cervical composite restored after careful moisture control."),
                                new VisitItemSeed("Composite Filling", 44, ToothConditionStatus.Filled, 145m, "Occlusal composite restored with refined anatomy.")
                            ])
                    ],
                    UpcomingAppointments:
                    [
                        new ScheduledAppointmentSeed(
                            "hygiene-review",
                            "DEMO-NORTH-001",
                            "N1",
                            SeedMoment(now, 0, 24, 17),
                            45,
                            AppointmentStatus.Scheduled,
                            "Six-month hygiene review scheduled after initial transfer to the clinic.")
                    ],
                    Xrays:
                    [
                        new XraySeed("laura-baseline", SeedMoment(now, -4, -10, 11), SeedMoment(now, 8, -10, 11), "Baseline bitewing after insurer-driven clinic change.")
                    ])
            ]);
    }
}

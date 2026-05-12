# Modules.GymManagement - Application

Phase 18 starts the GymManagement mediator surface with member CRUD endpoint
adapters.

Current responsibilities:
- handle `Modules.GymManagement.Contracts` member messages
- preserve existing member API routes and DTOs through mediator dispatch
- keep tenant authorization on the existing member workflow path during the
  transition

Future responsibilities:
- platform service for system-level gym registration, billing, and snapshots
- staff, contracts, vacations, and shifts
- maintenance, equipment, equipment models, opening hours, and gym settings
- subscription tier limit checks
- audit log writes through a mediator command surface

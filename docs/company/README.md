# Company, Calendar and Schedules

`CompanyController`, `CompanyMasterController`, and `CalendarController` own company details, departments, job titles, module access, custom attributes, calendar data, and office schedule configuration. `WeeklyOffSettingsController` and schedule services participate in the scheduling boundary. Existing references: `../company-module-current-flow.md`, `../calendar-module-current-flow.md`, and `../office-schedule-shifts.md`.

Company is the tenant boundary. Verify every service query derives its company from current context rather than request data.

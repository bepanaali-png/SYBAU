using MedicalApi.Models;

// Тестовые данные (задание 7): после запуска сервера в нём сразу есть пациенты, справки и приёмы.
// Отключается настройкой "SeedTestData": false в appsettings.json
public static class SeedData
{
    public static void Load()
    {
        if (PatientsController.Patients.Count > 0)
        {
            return;
        }

        Patient aigul = AddPatient("Айгуль Сыдыкова", new DateTime(1995, 3, 12), "female", "+996 555 123 456", "Бишкек, ул. Токтогула 12", "KG1000123", false);
        Patient bekzat = AddPatient("Бекзат Омуров", new DateTime(2014, 7, 21), "male", "+996 700 234 567", "Бишкек, мкр Асанбай 5", "KG1000124", true);
        Patient alisa = AddPatient("Алиса Иванова", new DateTime(2000, 5, 15), "female", "+996 777 345 678", "Бишкек, пр. Мира 40", "KG1000125", false);
        Patient timur = AddPatient("Тимур Асанов", new DateTime(1988, 11, 2), "male", "+996 505 456 789", "Бишкек, ул. Ахунбаева 7", "KG1000126", false);
        AddPatient("Нурайым Касымова", new DateTime(2010, 1, 30), "female", "+996 550 567 890", "Бишкек, ул. Фучика 21", "KG1000127", false);

        AddSpravka(aigul, 1, 10);
        AddSpravka(aigul, 2, 4);
        AddSpravka(bekzat, 4, 7);
        AddSpravka(bekzat, 3, 2);
        AddSpravka(alisa, 1, 1);
        AddSpravka(alisa, 5, 0);
        AddSpravka(timur, 6, 3);

        AddAppointment(aigul, "Рахимов Алмаз Куванович", "терапевт", WorkDay(1).AddHours(10), "Плановый осмотр для справки", "scheduled");
        AddAppointment(bekzat, "Омурбекова Нурия Таировна", "педиатр", WorkDay(2).AddHours(11), "Осмотр перед лагерем", "scheduled");
        AddAppointment(alisa, "Сатыбалдиев Руслан Эрнисович", "окулист", WorkDay(3).AddHours(14), "Проверка зрения для водительских прав", "scheduled");
        AddAppointment(timur, "Рахимов Алмаз Куванович", "терапевт", DateTime.Today.AddDays(-2).AddHours(9), "Жалобы на давление", "completed");
    }

    // Ближайший рабочий день через daysAhead дней (воскресенье пропускаем)
    private static DateTime WorkDay(int daysAhead)
    {
        DateTime day = DateTime.Today.AddDays(daysAhead);
        if (day.DayOfWeek == DayOfWeek.Sunday)
        {
            day = day.AddDays(1);
        }
        return day;
    }

    private static Patient AddPatient(string name, DateTime birthDate, string gender, string phone, string address, string insurance, bool allergies)
    {
        Patient patient = new Patient
        {
            Id = PatientsController.NextId++,
            Name = name,
            BirthDate = birthDate,
            Gender = gender,
            Phone = phone,
            Address = address,
            InsuranceNumber = insurance,
            HasAllergies = allergies,
            RegisteredAt = DateTime.Now
        };
        PatientsController.Patients.Add(patient);
        return patient;
    }

    private static void AddSpravka(Patient patient, int typeId, int daysAgo)
    {
        SpravkaType type = SpravkaTypesController.Types.First(t => t.Id == typeId);
        DateTime issuedAt = DateTime.Now.AddDays(-daysAgo);

        Spravka spravka = new Spravka
        {
            Id = SpravkiController.NextId++,
            Name = type.Name,
            Price = type.Price,
            PatientId = patient.Id,
            SpravkaTypeId = type.Id,
            IssuedAt = issuedAt,
            ValidUntil = issuedAt.AddDays(type.ValidDays)
        };
        SpravkiController.Spravki.Add(spravka);
    }

    private static void AddAppointment(Patient patient, string doctor, string specialty, DateTime start, string reason, string status)
    {
        Appointment appointment = new Appointment
        {
            Id = AppointmentsController.NextId++,
            PatientId = patient.Id,
            DoctorName = doctor,
            Specialty = specialty,
            StartTime = start,
            DurationMinutes = 30,
            Reason = reason,
            Status = status,
            CreatedAt = DateTime.Now
        };
        AppointmentsController.Appointments.Add(appointment);
    }
}

using Microsoft.EntityFrameworkCore;
using MedicalSystem.Models;

namespace MedicalSystem.Data;

public class MedicalDbContext : DbContext
{
    public MedicalDbContext(DbContextOptions<MedicalDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionDetail> PrescriptionDetails => Set<PrescriptionDetail>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<ConversationHistory> ConversationHistories => Set<ConversationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置实体关系
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Schedule)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Patient)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Doctor)
            .WithMany(d => d.MedicalRecords)
            .HasForeignKey(m => m.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.MedicalRecord)
            .WithMany(m => m.Prescriptions)
            .HasForeignKey(p => p.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrescriptionDetail>()
            .HasOne(pd => pd.Prescription)
            .WithMany(p => p.Details)
            .HasForeignKey(pd => pd.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrescriptionDetail>()
            .HasOne(pd => pd.Medicine)
            .WithMany(m => m.PrescriptionDetails)
            .HasForeignKey(pd => pd.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Charge>()
            .HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Charge>()
            .HasOne(c => c.MedicalRecord)
            .WithOne(m => m.Charge)
            .HasForeignKey<Charge>(c => c.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置索引
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.IdCard)
            .IsUnique();

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Phone);

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => new { s.DoctorId, s.Date, s.TimeSlot })
            .IsUnique();

        modelBuilder.Entity<ConversationHistory>()
            .HasIndex(c => c.SessionId);

        modelBuilder.Entity<ConversationHistory>()
            .HasIndex(c => c.CreatedAt);
    }
}

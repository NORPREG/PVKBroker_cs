using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PvkBroker.Configuration;
using Serilog;

namespace PvkBroker.Kodeliste
{
    public class KodelisteDbContext : DbContext
    {
        public DbSet<Registry> Registries { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientID> PatientIDs { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Study> Studies { get; set; }
        public DbSet<Export> Exports { get; set; }
        public DbSet<PatientExport> PatientExports { get; set; }
        public DbSet<DataStatus> DataStatuses { get; set; }


        public KodelisteDbContext(DbContextOptions<KodelisteDbContext> options) : base(options) { }
        public KodelisteDbContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string Server = ConfigurationValues.KodelisteServer;
            string DatabaseName = ConfigurationValues.KodelisteDbName;
            string UserName = ConfigurationValues.KodelisteUsername;
            string Password = ConfigurationValues.KodelistePassword;

            if (string.IsNullOrEmpty(DatabaseName)) { return; }
            string connstring = $"Server={Server};Database={DatabaseName};Trusted_Connection=True"; //  User Id={UserName};Password={Password}";

            optionsBuilder.UseSqlServer(connstring);
        }
        s

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .HasKey(r => r.patient_key);

            // Registry (one) <-> (many) Patients
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.registry)
                .WithMany(r => r.patients)
                .HasForeignKey(p => p.fk_registry_id);

            // Registry (many) <-> RegistryExport <-> Export (many)
            modelBuilder.Entity<Registry>()
                .HasMany(r => r.registry_exports)
                .WithOne(re => re.registry)
                .HasForeignKey(re => re.fk_registry_id);
            modelBuilder.Entity<Export>()
                .HasMany(e => e.registry_exports)
                .WithOne(re => re.export)
                .HasForeignKey(re => re.fk_export_id);

            // Patient (one) <-> (many) PatientIDs
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.patient_ids)
                .WithOne(pid => pid.patient)
                .HasForeignKey(pid => pid.fk_patient_key);

            // Patient (one) <-> (many) Addresses
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.addresses)
                .WithOne(a => a.patient)
                .HasForeignKey(a => a.fk_patient_key);

            // Patient (one) <-> (many) Courses
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.courses)
                .WithOne(c => c.patient)
                .HasForeignKey(c => c.fk_patient_key);

            // Patient (one) <-> (many) PatientExports
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.patient_exports)
                .WithOne(pe => pe.patient)
                .HasForeignKey(pe => pe.fk_patient_key);

            // Course (one) <-> (many) PatientExports
            modelBuilder.Entity<Course>()
                .HasMany(c => c.patient_exports)
                .WithOne(pe => pe.course)
                .HasForeignKey(pe => pe.fk_course_id);

            // Export (one) <-> (many) PatientExports
            modelBuilder.Entity<Export>()
                .HasMany(e => e.patient_exports)
                .WithOne(pe => pe.export)
                .HasForeignKey(pe => pe.fk_export_id);

            modelBuilder.Entity<PatientExport>()
            .HasIndex(pe => pe.pseudo_key)
            .IsUnique();

            // Export (many) <-> (one) Study
            modelBuilder.Entity<Export>()
                .HasOne(e => e.study)
                .WithMany(s => s.export_list)
                .HasForeignKey(e => e.fk_study_id);

            // DataStatus (one) <-> (one) Course
            modelBuilder.Entity<DataStatus>()
                .HasOne(ds => ds.course)
                .WithOne(c => c.data_status)
                .HasForeignKey(ds => ds.fk_course_id);

            // PvkEvent (many) <-> (one) Patient
            modelBuilder.Entity<PvkEvent>()
                .HasOne(pe => pe.patient)
                .WithMany(p => p.pvk_events)
                .HasForeignKey(pe => pe.fk_patient_key);

            // PvkEvents (one) <-> (many) PvkSync
            modelBuilder.Entity<PvkEvent>()
               .HasOne(pe => pe.pvk_sync)
               .WithMany(ps => ps.pvk_event)
               .HasForeignKey(pe => pe.fk_sync_id);
        }
    }
}
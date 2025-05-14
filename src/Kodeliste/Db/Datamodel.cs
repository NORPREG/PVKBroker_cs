using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PvkBroker.Configuration;
using Serilog;

// This file is manually in sync with the SQLAlchemy models in the Python project.
// https://github.com/NORPREG/DICOMBroker/blob/main/Dataclasses/KodelisteDataclass.py

namespace PvkBroker.Kodeliste.Db
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
        }s

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
        }
    }

    public class Registry
    {
        public int id { get; set; }
        public string name { get; set; } // KREST-XXX, NORPREG
        public List<Patient> patients { get; set; } // list of patients in this registry
        public List<RegistryExport> registry_exports { get; set; } // registry export
    }

    public class Patient
    {
        public int patient_key { get; set; } // registry-wide pseudonymization key for the patient
        public DateTime dt_added { get; set; } // datetime added to the registry

        public int fk_registry_id { get; set; } // foreign key to the registry
        public Registry registry { get; set; }

        public List<PatientID> patient_ids { get; set; } // list of patient IDs
        public List<Address> addresses { get; set; } // list of addresses
        public List<Course> courses { get; set; } // list of courses
        public List<PatientExport> patient_exports { get; set; } // list of patient exports

        public string name { get; set; } // encrypted name
        public string birth_date { get; set; } // encrypted birth date
        public string ois_patient_id { get; set; } // encrypted OIS patient ID
        public string epj_patient_id { get; set; } // encrypted EPJ patient ID
    }

    public class PatientID
    {
        public int id { get; set; }
        public Patient patient { get; set; }
        public int fk_patient_key { get; set; } // foreign key to the patient
        public DateTime dt_added { get; set; } // datetime added to the registry
        public string fnr { get; set; } // encrypted fnr
        public string fnr_type { get; set; } // encrypted fnr type
    }

    public class Address
    {
        public int id { get; set; }
        public DateTime dt_added { get; set; } // date added to the registry
        public int fk_patient_key { get; set; } // foreign key to the patient
        public Patient patient { get; set; }

        public string zip_code { get; set; } // encrypted zip code
        public string bydel { get; set; } // encrypted bydel
        public string kommune_nr { get; set; } // encrypted kommune number
    }

    public class Course
    {
        public int id { get; set; }
        public DateTime dt_added { get; set; } // datetime added to the registry
        public int fk_patient_key { get; set; } // foreign key to the patient
        public Patient patient { get; set; }
        public List<PatientExport> patient_exports { get; set; } // list of patient exports
        public int fk_datastatus_id { get; set; } // foreign key to the data status
        public DataStatus data_status { get; set; }
        public string ois_course_id { get; set; } // encrypted OIS course ID
        public string epj_course_id { get; set; } // encrypted EPJ course ID
    }

    public class Study
    {
        public int id { get; set; }
        public string conquest_name { get; set; }
        public string description { get; set; }
        public string contact_person { get; set; }
        public string institution { get; set; }
        public string email { get; set; }
        public DateTime store_until { get; set; }

        public List<Export> exports { get; set; }
    }

    public class Export // single export action
    {
        public int id { get; set; }
        public int fk_study_id { get; set; } // foreign key to the study
        public Study study { get; set; }
        public List<PatientExport> patient_exports { get; set; } // list of patients in this export
        public List<RegistryExport> registry_exports { get; set; } // list of registries in this export
        public DateTime export_date { get; set; }
        public string contact_person { get; set; }
        public string institution { get; set; }
        public string email { get; set; }
        public string mechanism { get; set; }
        public bool is_pseudo { get; set; }
    }

    public class PatientExport // many-to-many collection of patients with primary definition of pseudo keys for a given export action
    {
        public int id { get; set; }
        public int fk_patient_key { get; set; } // foreign key to the patient
        public Patient patient { get; set; }
        public int fk_course_id { get; set; } // foreign key to the course
        public Course course { get; set; }
        public int fk_export_id { get; set; } // foreign key to the export
        public Export export { get; set; }
        public string pseudo_key { get; set; } // encrypted pseudo key for this patient / export action
    }

    public class RegistryExport // many-to-many collection of registry / export actions (e.g. from 2x KREST-XXX)
    {
        public int id { get; set; }
        public int fk_registry_id { get; set; } // foreign key to the registry
        public Registry registry { get; set; }
        public int fk_export_id { get; set; } // foreign key to the export
        public Export export { get; set; }
    }

    public class DataStatus
    {
        public int id { get; set; }
        public int fk_course_id { get; set; } // foreign key to the course
        public Course course { get; set; }
        public int epj_status { get; set; } // encrypted EPJ status
        public int dicom_status { get; set; } // encrypted DICOM status
        public int consent_status { get; set; } // encrypted consent status
        public int prom_status { get; set; } // encrypted PROM status
    }
}
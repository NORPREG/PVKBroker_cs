using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PvkBroker.Configuration;
using Serilog;
using System;

// This file is manually in sync with the SQLAlchemy models in the Python project.
// https://github.com/NORPREG/DICOMBroker/blob/main/Dataclasses/KodelisteDataclass.py

namespace PvkBroker.Kodeliste
{
    public class Registry
    {
        public int id { get; set; }
        public string? name { get; set; } // KREST-XXX, NORPREG
        public List<Patient>? patients { get; set; } // list of patients in this registry. Not for NORPREG, that'd need more modelling (many-to-many)
        public List<RegistryExport>? registry_exports { get; set; } // registry export
    }

    public class Patient
    {
        public int id { get; set; }
        public string? patient_key { get; set; } // registry-wide pseudonymization key for the patient
        public DateTime dt_added { get; set; } // datetime added to the registry

        public int fk_registry_id { get; set; } // foreign key to the registry
        public Registry? registry { get; set; } // registry this patient belongs to originally (KREST)

        public List<PatientID>? patient_ids { get; set; } // list of patient IDs
        public List<Address>? addresses { get; set; } // list of addresses
        public List<Course>? courses { get; set; } // list of courses
        public List<PatientExport>? patient_exports { get; set; } // list of patient exports
        public List<PvkEvent>? pvk_events { get; set; } // List of PVK events

        public string? name_aes { get; set; } // encrypted name
        public string? birth_date_aes { get; set; } // encrypted birth date
        public string? ois_patient_id_aes { get; set; } // encrypted OIS patient ID
        public string? epj_patient_id_aes { get; set; } // encrypted EPJ patient ID
    }

    public class PatientID
    {
        public int id { get; set; }
        public Patient? patient { get; set; }
        public int fk_patient_id { get; set; } // foreign key to the patient
        public DateTime dt_added { get; set; } // datetime added to the registry
        public string? fnr_aes { get; set; } // encrypted fnr
        public string? fnr_type { get; set; } // patient ID type
    }

    public class Address
    {
        public int id { get; set; }
        public DateTime dt_added { get; set; } // date added to the registry
        public int fk_patient_id { get; set; } // foreign key to the patient
        public Patient? patient { get; set; }

        public string? zip_code_aes { get; set; } // encrypted zip code
        public string? bydel_aes { get; set; } // encrypted bydel
        public string? kommune_nr_aes { get; set; } // encrypted kommune number
    }

    public class Course
    {
        public int id { get; set; }
        public DateTime dt_added { get; set; } // datetime added to the registry
        public int fk_patient_id { get; set; } // foreign key to the patient
        public Patient? patient { get; set; }
        public List<PatientExport>? patient_exports { get; set; } // list of patient exports
        public int fk_datastatus_id { get; set; } // foreign key to the data status
        public DataStatus? data_status { get; set; }
        public string? ois_course_id { get; set; } // encrypted OIS course ID
        public string? epj_course_id { get; set; } // encrypted EPJ course ID
    }

    public class Study
    {
        public int id { get; set; }
        public string? conquest_name { get; set; } // Conquest name for the study
        public string? description_aes { get; set; } // Encrypted description
        public string? contact_person_aes { get; set; } // Encrypted contact person name
        public string? institution_aes { get; set; } // Encrypted institution name
        public string? email_aes { get; set; } // Encrypted email
        public DateTime store_until { get; set; }

        public List<Export>? exports { get; set; }
    }

    public class Export // single export action
    {
        public int id { get; set; }
        public int fk_study_id { get; set; } // foreign key to the study
        public Study? study { get; set; }
        public List<PatientExport>? patient_exports { get; set; } // list of patients in this export
        public List<RegistryExport>? registry_exports { get; set; } // list of registries in this export
        public DateTime export_date { get; set; }
        public string? contact_person_aes { get; set; } // Encrypted contact person
        public string? institution_aes { get; set; } // Encrypted institution
        public string? email_aes { get; set; } // Encrypted email
        public string? mechanism { get; set; }
        public bool is_pseudo { get; set; }
    }

    public class PatientExport // many-to-many collection of patients with primary definition of pseudo keys for a given export action
    {
        public int id { get; set; }
        public int fk_patient_id { get; set; } // foreign key to the patient
        public Patient? patient { get; set; }
        public int fk_course_id { get; set; } // foreign key to the course
        public Course? course { get; set; }
        public int fk_export_id { get; set; } // foreign key to the export
        public Export? export { get; set; }
        public string? pseudo_key_aes { get; set; } // encrypted pseudo key for this patient / export action
    }

    public class RegistryExport // many-to-many collection of registry / export actions (e.g. from 2x KREST-XXX)
    {
        public int id { get; set; }
        public int fk_registry_id { get; set; } // foreign key to the registry
        public Registry? registry { get; set; }
        public int fk_export_id { get; set; } // foreign key to the export
        public Export? export { get; set; }
    }

    public class DataStatus
    {
        public int id { get; set; }
        public int fk_course_id { get; set; } // foreign key to the course
        public Course? course { get; set; }
        public int epj_status_aes { get; set; } // encrypted EPJ status
        public int dicom_status_aes { get; set; } // encrypted DICOM status
        public int consent_status_aes { get; set; } // encrypted consent status
        public int prom_status_aes { get; set; } // encrypted PROM status
    }

    public class PvkEvent
    {
        public int id { get; set; }
        public DateTime event_time { get; set; }
        public int fk_patient_id { get; set; } // foreign key to the patient
        public Patient? patient { get; set; }
        public int fk_sync_id { get; set; } // foreign key to the sync
        public PvkSync? pvk_sync { get; set; }
        public string? is_reserved_aes { get; set; } // encrypted reservation status ("1" = reserved, "0" = not reserved)
    }

    public class PvkSync
    {
        public int id { get; set; }
        public List<PvkEvent>? pvk_events { get; set; }
        public DateTime dt_sync { get; set; } // datetime of the last sync
        public int new_reservations { get; set; } // status of the sync
        public int withdrawn_reservations { get; set; } // status of the sync
        public string? error_message { get; set; } // error message if any
    }
}
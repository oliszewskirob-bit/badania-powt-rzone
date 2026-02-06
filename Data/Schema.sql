PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  DisplayName TEXT NOT NULL,
  Role TEXT NOT NULL CHECK(Role IN ('user','manager','admin')),
  PasswordHash BLOB NOT NULL,
  PasswordSalt BLOB NOT NULL,
  MustChangePassword INTEGER NOT NULL DEFAULT 0,
  IsActive INTEGER NOT NULL DEFAULT 1,
  CreatedAt TEXT NOT NULL,
  CreatedBy TEXT NOT NULL,
  UpdatedAt TEXT NULL,
  UpdatedBy TEXT NULL
);

CREATE TABLE IF NOT EXISTS Reasons (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Modality TEXT NOT NULL CHECK(Modality IN ('CT','MR')),
  Name TEXT NOT NULL,
  IsActive INTEGER NOT NULL DEFAULT 1,
  UNIQUE(Modality, Name)
);

CREATE TABLE IF NOT EXISTS RepeatEvents (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Modality TEXT NOT NULL CHECK(Modality IN ('CT','MR')),
  Device TEXT NOT NULL,
  EventType TEXT NOT NULL CHECK(EventType IN ('repeat','supplement','contrast_extravasation')),
  PatientName TEXT NOT NULL,
  PatientId TEXT NOT NULL,
  Accession TEXT NULL,
  FirstPartDateTime TEXT NOT NULL,
  FixRequestedByDoctor TEXT NOT NULL,
  TechFirstPart TEXT NOT NULL,
  Nurse TEXT NOT NULL,
  ReasonId INTEGER NOT NULL,
  ReasonOtherText TEXT NULL,
  FixDateTime TEXT NULL,
  ExtraMinutes INTEGER NULL,
  Status TEXT NOT NULL CHECK(Status IN ('new','in_progress','closed')) DEFAULT 'new',
  Outcome TEXT NULL,
  Description TEXT NULL,
  CorrectiveAction TEXT NULL,
  Notes TEXT NULL,
  IsContrastExtravasation INTEGER NOT NULL DEFAULT 0,
  ContrastCannula TEXT NULL,
  ContrastType TEXT NULL,
  ContrastFlow TEXT NULL,
  ContrastVolume TEXT NULL,
  ContrastVisible INTEGER NULL,
  WardNotified INTEGER NULL,
  PatientInstructions INTEGER NULL,
  ContrastAdditionalInfo TEXT NULL,

  CreatedAt TEXT NOT NULL,
  CreatedBy TEXT NOT NULL,
  UpdatedAt TEXT NULL,
  UpdatedBy TEXT NULL,
  ClosedAt TEXT NULL,
  ClosedBy TEXT NULL,
  RowVersion INTEGER NOT NULL DEFAULT 1,
  IsDeleted INTEGER NOT NULL DEFAULT 0,
  DeletedAt TEXT NULL,
  DeletedBy TEXT NULL,

  FOREIGN KEY(ReasonId) REFERENCES Reasons(Id)
);

CREATE TABLE IF NOT EXISTS AuditLog (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EventId INTEGER NOT NULL,
  Action TEXT NOT NULL,
  FieldName TEXT NOT NULL,
  OldValue TEXT NOT NULL,
  NewValue TEXT NOT NULL,
  ChangedAt TEXT NOT NULL,
  ChangedBy TEXT NOT NULL,
  Machine TEXT NOT NULL,
  AppVersion TEXT NOT NULL,
  FOREIGN KEY(EventId) REFERENCES RepeatEvents(Id)
);

CREATE TABLE IF NOT EXISTS EventAttachments (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EventId INTEGER NOT NULL,
  FileName TEXT NOT NULL,
  StoredPath TEXT NOT NULL,
  AddedAt TEXT NOT NULL,
  AddedBy TEXT NOT NULL,
  FOREIGN KEY(EventId) REFERENCES RepeatEvents(Id)
);

-- Seed CT
INSERT INTO Reasons (Modality, Name)
SELECT 'CT', 'Wynaczynienie kontrastu'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='CT' AND Name='Wynaczynienie kontrastu');

INSERT INTO Reasons (Modality, Name)
SELECT 'CT', 'Artefakty ruchowe'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='CT' AND Name='Artefakty ruchowe');

INSERT INTO Reasons (Modality, Name)
SELECT 'CT', 'Błąd aparatu TK'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='CT' AND Name='Błąd aparatu TK');

INSERT INTO Reasons (Modality, Name)
SELECT 'CT', 'Niewłaściwy zakres badania'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='CT' AND Name='Niewłaściwy zakres badania');

INSERT INTO Reasons (Modality, Name)
SELECT 'CT', 'Błąd strzykawki / iniektora'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='CT' AND Name='Błąd strzykawki / iniektora');

INSERT INTO Reasons (Modality, Name)
SELECT 'CT', 'Inne (doprecyzuj)'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='CT' AND Name='Inne (doprecyzuj)');

-- Seed MR
INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Nie widać kontrastu'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Nie widać kontrastu');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Wynaczynienie kontrastu'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Wynaczynienie kontrastu');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Dorobienie kontrastu'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Dorobienie kontrastu');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Błąd aparatu MR'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Błąd aparatu MR');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Błąd strzykawki automatycznej'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Błąd strzykawki automatycznej');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Niewłaściwy zakres badania'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Niewłaściwy zakres badania');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Niewłaściwe sekwencje'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Niewłaściwe sekwencje');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Artefakty ruchowe'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Artefakty ruchowe');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Artefakty inne'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Artefakty inne');

INSERT INTO Reasons (Modality, Name)
SELECT 'MR', 'Inne (doprecyzuj)'
WHERE NOT EXISTS (SELECT 1 FROM Reasons WHERE Modality='MR' AND Name='Inne (doprecyzuj)');

-- Create FieldConfigurations table
CREATE TABLE IF NOT EXISTS FieldConfigurations (
    FieldConfigurationId INTEGER PRIMARY KEY AUTOINCREMENT,
    DocumentType TEXT NOT NULL,
    FieldName TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    DisplayName TEXT NOT NULL,
    Description TEXT,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    CreatedBy INTEGER NOT NULL,
    UpdatedAt TEXT,
    UpdatedBy INTEGER,
    UNIQUE(DocumentType, FieldName)
);

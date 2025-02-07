import sqlite3
import os
from datetime import datetime

# migrates the old manictime data to the current layout

# File paths for the old and new databases
old_db_path = r'./manictime.db'
new_db_path = r'./new.db'

# New schema SQL
new_schema_sql = '''
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "Settings" (
    "SettingName" TEXT NOT NULL CONSTRAINT "PK_Settings" PRIMARY KEY,
    "Value" TEXT NOT NULL,
    "CreationDate" TEXT NULL
);
CREATE TABLE IF NOT EXISTS "Tags" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tags" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NULL,
    "Inactive" INTEGER NOT NULL DEFAULT 0,
    "CreationDate" TEXT NULL
);
CREATE TABLE IF NOT EXISTS "Filters" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Filters" PRIMARY KEY AUTOINCREMENT,
    "FilterType" TEXT NULL,
    "FieldType" TEXT NULL,
    "FieldValue" TEXT NULL,
    "TagId" INTEGER NOT NULL,
    "Inactive" INTEGER NOT NULL DEFAULT 0,
    "CreationDate" TEXT NULL,
    CONSTRAINT "FK_Filters_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "TagSessions" (
    "SessionId" INTEGER NOT NULL CONSTRAINT "PK_TagSessions" PRIMARY KEY AUTOINCREMENT,
    "TagId" INTEGER NOT NULL,
    "StartTime" TEXT NOT NULL,
    "LastUpdateTime" TEXT NOT NULL,
    "EndTime" TEXT NULL,
    "CreationDate" TEXT NULL,
    CONSTRAINT "FK_TagSessions_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "TagSessionSummary" (
    "SummaryId" INTEGER NOT NULL CONSTRAINT "PK_TagSessionSummary" PRIMARY KEY AUTOINCREMENT,
    "Day" TEXT NOT NULL,
    "TagId" INTEGER NOT NULL,
    "Seconds" REAL NOT NULL,
    "CreationDate" TEXT NULL,
    CONSTRAINT "FK_TagSessionSummary_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_Filters_TagId" ON "Filters" ("TagId");
CREATE INDEX "IX_TagSessions_TagId" ON "TagSessions" ("TagId");
CREATE INDEX "IX_TagSessionSummary_TagId" ON "TagSessionSummary" ("TagId");
'''

def create_new_db():
    # Remove the new DB if it exists to start fresh
    if os.path.exists(new_db_path):
        os.remove(new_db_path)

    # Create the new database and apply the schema
    conn = sqlite3.connect(new_db_path)
    cursor = conn.cursor()
    cursor.executescript(new_schema_sql)
    conn.commit()
    conn.close()

def migrate_tags():
    conn_old = sqlite3.connect(old_db_path)
    conn_new = sqlite3.connect(new_db_path)
    
    old_cursor = conn_old.cursor()
    new_cursor = conn_new.cursor()
    
    # Fetch tags from the old database
    old_cursor.execute("SELECT TagId, Name FROM Tag")
    tags = old_cursor.fetchall()

    # Insert tags into the new database
    for tag in tags:
        tag_id, tag_name = tag
        inactive = 1  # Set inactive to 1 as per requirement
        creation_date = datetime.now().strftime("%Y-%m-%d %H:%M:%S.0000000")
        
        new_cursor.execute('''
            INSERT INTO Tags (Id, Name, Inactive, CreationDate)
            VALUES (?, ?, ?, ?)
        ''', (tag_id, tag_name, inactive, creation_date))
    
    conn_new.commit()
    conn_old.close()
    conn_new.close()

def migrate_summary():
    conn_old = sqlite3.connect(old_db_path)
    conn_new = sqlite3.connect(new_db_path)
    
    old_cursor = conn_old.cursor()
    new_cursor = conn_new.cursor()
    
    # Fetch summary data from old database
    old_cursor.execute("SELECT SummaryId, StartDate, Duration, TagId FROM Summary")
    summaries = old_cursor.fetchall()
    
    # Calculate total duration per tag per day
    summary_data = {}
    
    for summary in summaries:
        summary_id, start_date, duration, tag_id = summary
        day = datetime.strptime(start_date, "%Y-%m-%d %H:%M:%S").strftime("%Y-%m-%d 00:00:00.0000000")

        
        if (tag_id, day) not in summary_data:
            summary_data[(tag_id, day)] = 0
        
        summary_data[(tag_id, day)] += duration
    
    # Insert summary into the new database
    for (tag_id, day), total_duration in summary_data.items():
        new_cursor.execute('''
            INSERT INTO TagSessionSummary (Day, TagId, Seconds, CreationDate)
            VALUES (?, ?, ?, ?)
        ''', (day, tag_id, total_duration, day))
    
    conn_new.commit()
    conn_old.close()
    conn_new.close()

if __name__ == '__main__':
    create_new_db()
    migrate_tags()
    migrate_summary()
    print("Migration completed successfully.")

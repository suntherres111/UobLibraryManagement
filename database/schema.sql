/*
  Library Management System - SQL Server schema (reference script)

  The API creates this schema automatically on first run (EF Core EnsureCreated),
  so you do not need to run this script. It documents the design for review.
*/

CREATE TABLE dbo.Books
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
    Isbn            VARCHAR(13)       NOT NULL,
    Title           NVARCHAR(200)     NOT NULL,
    Author          NVARCHAR(150)     NOT NULL,
    Publisher       NVARCHAR(150)     NULL,
    Genre           NVARCHAR(50)      NULL,
    PublishedYear   INT               NOT NULL,
    TotalCopies     INT               NOT NULL,
    AvailableCopies INT               NOT NULL,   -- optimistic concurrency token
    IsActive        BIT               NOT NULL,   -- soft delete
    CreatedAtUtc    DATETIME2         NOT NULL,
    UpdatedAtUtc    DATETIME2         NULL,
    CONSTRAINT CK_Books_TotalCopies     CHECK (TotalCopies >= 1),
    CONSTRAINT CK_Books_AvailableCopies CHECK (AvailableCopies >= 0 AND AvailableCopies <= TotalCopies)
);
CREATE UNIQUE INDEX IX_Books_Isbn   ON dbo.Books (Isbn);
CREATE INDEX        IX_Books_Title  ON dbo.Books (Title);
CREATE INDEX        IX_Books_Author ON dbo.Books (Author);

CREATE TABLE dbo.Members
(
    Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Members PRIMARY KEY,
    FirstName      NVARCHAR(100)     NOT NULL,
    LastName       NVARCHAR(100)     NOT NULL,
    Email          NVARCHAR(256)     NOT NULL,
    PhoneNumber    VARCHAR(20)       NULL,
    MembershipType NVARCHAR(20)      NOT NULL,   -- 'Standard' | 'Premium'
    IsActive       BIT               NOT NULL,   -- soft delete
    CreatedAtUtc   DATETIME2         NOT NULL,
    UpdatedAtUtc   DATETIME2         NULL
);
CREATE UNIQUE INDEX IX_Members_Email ON dbo.Members (Email);

CREATE TABLE dbo.Loans
(
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Loans PRIMARY KEY,
    BookId        INT               NOT NULL CONSTRAINT FK_Loans_Books_BookId     REFERENCES dbo.Books (Id),   -- ON DELETE NO ACTION
    MemberId      INT               NOT NULL CONSTRAINT FK_Loans_Members_MemberId REFERENCES dbo.Members (Id), -- ON DELETE NO ACTION
    BorrowedAtUtc DATETIME2         NOT NULL,
    DueAtUtc      DATETIME2         NOT NULL,
    ReturnedAtUtc DATETIME2         NULL,         -- NULL = still on loan
    FineAmount    DECIMAL(10,2)     NOT NULL,
    CreatedAtUtc  DATETIME2         NOT NULL,
    UpdatedAtUtc  DATETIME2         NULL,
    CONSTRAINT CK_Loans_DueAfterBorrow  CHECK (DueAtUtc > BorrowedAtUtc),
    CONSTRAINT CK_Loans_FineNotNegative CHECK (FineAmount >= 0)
);
-- A member can hold at most one open loan for the same book.
CREATE UNIQUE INDEX UX_Loans_Member_Book_Open ON dbo.Loans (MemberId, BookId) WHERE ReturnedAtUtc IS NULL;
CREATE INDEX IX_Loans_BookId                 ON dbo.Loans (BookId);
CREATE INDEX IX_Loans_MemberId_ReturnedAtUtc ON dbo.Loans (MemberId, ReturnedAtUtc);
CREATE INDEX IX_Loans_ReturnedAtUtc_DueAtUtc ON dbo.Loans (ReturnedAtUtc, DueAtUtc);

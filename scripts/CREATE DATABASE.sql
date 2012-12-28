IF OBJECT_ID(N'Torrents') IS NOT NULL
	DROP TABLE Torrents
GO

CREATE TABLE Torrents (
	TorrentID INT IDENTITY(1,1) PRIMARY KEY,
	ChannelID INT NOT NULL,
	Title VARCHAR(256) NOT NULL,
	DatePublished DATETIME NOT NULL,
	ResourceUri VARCHAR(1024) NOT NULL,
	TorrentUri VARCHAR(1024) NOT NULL,
	LocalPath VARCHAR(1024) NOT NULL,
	FileByteLength INT NOT NULL
)

IF OBJECT_ID(N'Channels') IS NOT NULL
	DROP TABLE Channels
GO

CREATE TABLE Channels (
	ChannelID INT IDENTITY(1,1) PRIMARY KEY,
	Title VARCHAR(256) NOT NULL,
	Host VARCHAR(256) NOT NULL,
	Url VARCHAR(1024) NOT NULL,
	Category VARCHAR(256) NOT NULL,
	DateCreated DATETIME NOT NULL DEFAULT(GETDATE())
)

INSERT INTO Channels (Title, Host, Url, Category) VALUES
('Mormon Messages','mormonchannel.org','http://www.mormonchannel.org/mormonmessages','Series'),
('Bible Videos','mormonchannel.org','http://www.mormonchannel.org/biblevideos','Series'),
('Conversations','mormonchannel.org','http://www.mormonchannel.org/conversations','Series'),
('Enduring It Well','mormonchannel.org','http://www.mormonchannel.org/enduring-it-well','Series'),
('Gospel Solutions for Families','mormonchannel.org','http://www.mormonchannel.org/gospel-solutions-for-families','Series'),
('Mormon Channel Daily','mormonchannel.org','http://www.mormonchannel.org/mormon-channel-daily','Series'),
('Relief Society','mormonchannel.org','http://www.mormonchannel.org/relief-society','Series'),
('Stories from General Conference','mormonchannel.org','http://www.mormonchannel.org/stories-from-general-conference','Series'),
('Tech Savvy','mormonchannel.org','http://www.mormonchannel.org/tech-savvy','Series'),
('Teaching, No Greater Call','mormonchannel.org','http://www.mormonchannel.org/teaching-no-greater-call','Series'),
('We Will Stand','mormonchannel.org','http://www.mormonchannel.org/we-will-stand','Series'),
('Weekly Edition','mormonchannel.org','http://www.mormonchannel.org/weekly-edition','Series'),
('Mormon Messages For Youth','mormonchannel.org','http://www.mormonchannel.org/mormon-messages-for-youth','Series'),
('I''m a Mormon ','mormonchannel.org','http://www.mormonchannel.org/our-people','Series'),
('Archives ','mormonchannel.org','http://www.mormonchannel.org/archives','Series'),
('O Come, Emmanuel - Christmas Version - ThePianoGuys','mormonchannel.org','http://www.mormonchannel.org/more-videos?v=2008190983001','Collections'),
('First Presidency Christmas Devotional ','mormonchannel.org','http://www.mormonchannel.org/christmas-devotional-2012','Collections'),
('A Christmas Carol','mormonchannel.org','http://www.mormonchannel.org/a-christmas-carol','Collections'),
('The Coat','mormonchannel.org','http://www.mormonchannel.org/the-coat?v=1318641007001','Collections'),
('The Holy Spirit ','mormonchannel.org','http://www.mormonchannel.org/power-of-the-holy-spirit','Collections'),
('CES Devotionals ','mormonchannel.org','http://www.mormonchannel.org/ces-devotionals','Collections'),
('Addiction Recovery Program','mormonchannel.org','http://www.mormonchannel.org/addiction-recovery-program','Collections'),
('Provident Living','mormonchannel.org','http://www.mormonchannel.org/provident-living','Collections'),
('Mormon Channel Videos','mormonchannel.org','http://www.mormonchannel.org/more-videos','Collections'),
('Mormon Messages For Youth','mormonchannel.org','http://www.mormonchannel.org/mormon-messages-for-youth','Youth'),
('For the Youth','mormonchannel.org','http://www.mormonchannel.org/for-the-youth','Youth'),
('Youth Voices','mormonchannel.org','http://www.mormonchannel.org/youth-voices','Youth'),
('Youth Theme 2012: Arise and Shine Forth ','mormonchannel.org','http://www.mormonchannel.org/youth-theme-2012','Youth'),
('The Coat','mormonchannel.org','http://www.mormonchannel.org/more-videos?v=1318641007001','Children'),
('The Story of Jesus'' Birth','mormonchannel.org','http://www.mormonchannel.org/weekly-edition/14?v=2050186924001','Children'),
('Scripture Stories','mormonchannel.org','http://www.mormonchannel.org/scripture-stories','Children'),
('Book of Mormon Stories','mormonchannel.org','http://www.mormonchannel.org/book-of-mormon-stories','Children'),
('Old Testament Stories ','mormonchannel.org','http://www.mormonchannel.org/old-testament-stories','Children'),
('New Testament Stories ','mormonchannel.org','http://www.mormonchannel.org/new-testament-stories','Children'),
('Doctrine and Covenants Stories','mormonchannel.org','http://www.mormonchannel.org/doctrine-and-covenants-stories','Children'),
('Old Testament','mormonchannel.org','http://www.mormonchannel.org/scriptures/ot','Scriptures and Magazines'),
('New Testament','mormonchannel.org','http://www.mormonchannel.org/scriptures/nt','Scriptures and Magazines'),
('Book of Mormon','mormonchannel.org','http://www.mormonchannel.org/scriptures/bofm','Scriptures and Magazines'),
('Doctrine and Covenants','mormonchannel.org','http://www.mormonchannel.org/scriptures/dc-testament','Scriptures and Magazines'),
('Pearl of Great Price','mormonchannel.org','http://www.mormonchannel.org/scriptures/pgp','Scriptures and Magazines'),
('Ensign','mormonchannel.org','http://www.mormonchannel.org/magazines/ensign','Scriptures and Magazines'),
('New Era','mormonchannel.org','http://www.mormonchannel.org/magazines/new-era','Scriptures and Magazines'),
('Friend','mormonchannel.org','http://www.mormonchannel.org/magazines/friend','Scriptures and Magazines')











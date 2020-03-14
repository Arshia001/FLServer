﻿CREATE TABLE IF NOT EXISTS fl_config (
	key ascii,
	data text,

	PRIMARY KEY (key)
);

insert into queries(key, consistency_level, text) values 
(
	'fl_readConfig','eachquorum','
	SELECT
		data
	FROM fl_config
	WHERE
		key = :key;
');

insert into queries(key, consistency_level, text) values 
(
	'fl_updateConfig','eachquorum','
	UPDATE fl_config
	SET
		data = :data
	WHERE
		key = :key;
');

CREATE TABLE IF NOT EXISTS fl_groups (
	id int,
	name text,

	PRIMARY KEY (id)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

insert into queries(key, consistency_level, text) values 
(
	'fl_readGroups','all','
	SELECT
		*
	FROM fl_groups;
');

CREATE TABLE IF NOT EXISTS fl_categories (
	name text,
	group_id int,
	words map<text, frozen<set<text>>>,

	PRIMARY KEY (name)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

insert into queries(key, consistency_level, text) values 
(
	'fl_readCategories','all','
	SELECT
		*
	FROM fl_categories;
');

CREATE TABLE IF NOT EXISTS fl_suggested_categories ( // User-suggested categories
	owner_id uuid,
	name text,
	words set<text>,
	
	PRIMARY KEY (owner_id, name)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

CREATE TABLE IF NOT EXISTS fl_suggested_words ( // User-suggested words for **existing** categories
	owner_id uuid,
	category_name text,
	words set<text>,
	
	PRIMARY KEY (owner_id, category_name)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

insert into queries(key, consistency_level, text) values 
(
	'fl_UpsertSuggestedCategory','one','
	UPDATE fl_suggested_categories
	SET
		words = words + :words
	WHERE
		name = :name AND
		owner_id = :owner_id;
');

insert into queries(key, consistency_level, text) values 
(
	'fl_ReadSuggestedCategoriesByUser','one','
	SELECT
		*
	FROM fl_suggested_categories
	WHERE
		owner_id = :owner_id;
');

insert into queries(key, consistency_level, text) values 
(
	'fl_ReadSuggestedCategories','one','
	SELECT * FROM fl_suggested_categories;
');

insert into queries(key, consistency_level, text) values 
(
	'fl_UpsertSuggestedWords','one','
	UPDATE fl_suggested_words
	SET
		words = words + :words
	WHERE
		category_name = :category_name AND
		owner_id = :owner_id;
');

insert into queries(key, consistency_level, text) values 
(
	'fl_ReadSuggestedWordsByUser','one','
	SELECT
		*
	FROM fl_suggested_words
	WHERE
		owner_id = :owner_id;
');

insert into queries(key, consistency_level, text) values 
(
	'fl_ReadSuggestedWords','one','
	SELECT * FROM fl_suggested_words;
');
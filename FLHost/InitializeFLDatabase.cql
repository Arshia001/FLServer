CREATE TABLE IF NOT EXISTS fl_config (
	key int,
	data text,
	PRIMARY KEY (key)
);

CREATE TABLE IF NOT EXISTS fl_suggested_categories (
	owner_id uuid,
	name text,
	words set<text>,
	
	PRIMARY KEY (owner_id, name)
) WITH compression = {
	'class' : 'LZ4Compressor',
	'enabled' : true
};

CREATE TABLE IF NOT EXISTS fl_suggested_words (
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

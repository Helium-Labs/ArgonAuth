-- Table storing FIDO2 user data
CREATE TABLE `users` (
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(255) NOT NULL,
    `display_name` VARCHAR(255) NOT NULL,
    `user_id` BINARY(16) NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `user_id` (`user_id`),
    UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Without fk constraint for user_id, since Vitess doesn't support it.
-- Table storing FIDO2 credential data
CREATE TABLE `credentials` (
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `credential_id` BINARY(255) NOT NULL,
    `public_key` BLOB NOT NULL,
    `user_id` BINARY(16) NOT NULL,
    `signature_counter` INT(10) UNSIGNED NOT NULL,
    `cred_type` VARCHAR(255) NOT NULL,
    `reg_date` DATETIME NOT NULL,
    `aa_guid` BINARY(16) NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `credential_id` (`credential_id`),
    UNIQUE KEY `user_id` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
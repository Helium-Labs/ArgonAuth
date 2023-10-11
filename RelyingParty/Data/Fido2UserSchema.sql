-- Table storing FIDO2 user data
CREATE TABLE `users` (
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(255) NOT NULL,
    `display_name` VARCHAR(255) NOT NULL,
    `user_id` VARBINARY(255) NOT NULL,
    `json_metadata` VARCHAR(2048), 
    PRIMARY KEY (`id`),
    UNIQUE KEY `user_id` (`user_id`),
    UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Without fk constraint for user_id, since Vitess doesn't support it.
-- Table storing FIDO2 credential data
CREATE TABLE `credentials` (
   `credential_id` VARBINARY(255) PRIMARY KEY,
   `public_key` BLOB NOT NULL,
   `sign_count` INT(10) UNSIGNED NOT NULL,
   `transports` SET('usb', 'nfc', 'ble', 'internal') DEFAULT NULL, -- Using SET for multiple transport types
   `is_backup_eligible` BOOLEAN NOT NULL,
   `is_backed_up` BOOLEAN NOT NULL,
   `attestation_object` BLOB NOT NULL,
   `attestation_client_data_json` BLOB NOT NULL,
   `device_public_keys` JSON DEFAULT NULL, -- Using JSON to potentially store multiple device public keys
   `descriptor_type` ENUM('public-key', 'invalid') NOT NULL, -- Based on PublicKeyCredentialType enum
   `descriptor_id` VARBINARY(255) NOT NULL,
   `user_handle` VARBINARY(255) DEFAULT NULL,
   `attestation_format` VARCHAR(255) DEFAULT NULL,
   `reg_date` DATETIME NOT NULL,
   `aa_guid` BINARY(16) NOT NULL,
   UNIQUE KEY `credential_id` (`credential_id`),
   UNIQUE KEY `user_handle` (`user_handle`) -- Assuming user handle should be unique
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table for storing DIDT data
CREATE TABLE `DIDT` (
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `credential_id` VARBINARY(255) NOT NULL,
    `didt` BLOB NOT NULL,
    `signature` BLOB NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `credential_id` (`credential_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

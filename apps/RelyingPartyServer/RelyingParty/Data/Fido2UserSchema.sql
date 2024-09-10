-- Table storing FIDO2 user data
CREATE TABLE `users`
(
    `id`            INT(11)      NOT NULL AUTO_INCREMENT,
    `username`      VARCHAR(255) NOT NULL,
    `display_name`  VARCHAR(255) NOT NULL,
    `user_id_b64`   VARCHAR(255) NOT NULL,
    `json_metadata` VARCHAR(2048),
    PRIMARY KEY (`id`),
    UNIQUE KEY `user_id` (`user_id_b64`),
    UNIQUE KEY `username` (`username`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4;

-- Table storing FIDO2 credential data
CREATE TABLE `credentials`
(
    `id`                               INT(11)                        NOT NULL AUTO_INCREMENT,
    `credential_id_b64`                VARCHAR(255)                   NOT NULL,
    `public_key_b64`                   VARCHAR(1024)                  NOT NULL, -- Assuming a maximum length for public keys
    `sign_count`                       INT(10) UNSIGNED               NOT NULL,
    `transports`                       VARCHAR(255) DEFAULT NULL,               -- Using VARCHAR to store transport types as a comma-separated string
    `is_backup_eligible`               BOOLEAN                        NOT NULL,
    `is_backed_up`                     BOOLEAN                        NOT NULL,
    `attestation_object_b64`           TEXT                           NOT NULL, -- Using TEXT to store large base64 data
    `attestation_client_data_json_b64` TEXT                           NOT NULL, -- Using TEXT to store large base64 data
    `device_public_keys_b64`           TEXT         DEFAULT NULL,               -- Using TEXT to store JSON-encoded base64 data
    `descriptor_type`                  ENUM ('public-key', 'invalid') NOT NULL,
    `descriptor_id_b64`                VARCHAR(255)                   NOT NULL,
    `user_handle_b64`                  VARCHAR(255) DEFAULT NULL,
    `attestation_format`               VARCHAR(255) DEFAULT NULL,
    `reg_date`                         DATETIME                       NOT NULL,
    `aa_guid_b64`                      VARCHAR(36)                    NOT NULL, -- Assuming a maximum length for GUIDs
    PRIMARY KEY (`id`),
    UNIQUE KEY `credential_id` (`credential_id_b64`),
    UNIQUE KEY `user_handle` (`user_handle_b64`)                                -- Assuming user handle should be unique
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE auth_exchanges
(
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    code_hash      VARCHAR(255) NOT NULL,
    jwt_claims     TEXT         NOT NULL,
    timestamp      TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    username       VARCHAR(255) NOT NULL,
    state          VARCHAR(255) NOT NULL,
    code_challenge VARCHAR(255) NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE (code_hash)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4;

CREATE TABLE `email_verification_codes`
(
    `id` INT(11) NOT NULL AUTO_INCREMENT,
    `email` VARCHAR(255) NOT NULL,
    `code_hash` CHAR(64) NOT NULL, -- SHA256 hash is 64 characters long
    `timestamp` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `code_hash` (`code_hash`),
    UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

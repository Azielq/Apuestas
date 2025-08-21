-- Script to create ApiBet table
CREATE TABLE IF NOT EXISTS `ApiBet` (
    `ApiBetId` int NOT NULL AUTO_INCREMENT,
    `ApiEventId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `SportKey` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `TeamName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    `Odds` decimal(10,2) NOT NULL,
    `Stake` decimal(10,2) NOT NULL,
    `Payout` decimal(10,2) NOT NULL,
    `BetStatus` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'P',
    `Date` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `PaymentTransactionId` int NULL,
    `CreatedAt` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `UpdatedAt` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    CONSTRAINT `PK_ApiBet` PRIMARY KEY (`ApiBetId`),
    INDEX `IX_ApiBet_ApiEventId` (`ApiEventId`),
    INDEX `IX_ApiBet_BetStatus` (`BetStatus`),
    INDEX `IX_ApiBet_Date` (`Date`),
    INDEX `FK_ApiBet_PaymentTxn` (`PaymentTransactionId`),
    CONSTRAINT `FK_ApiBet_PaymentTransaction_PaymentTransactionId` FOREIGN KEY (`PaymentTransactionId`) REFERENCES `PaymentTransaction` (`PaymentTransactionId`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create junction table for many-to-many relationship with UserAccount
CREATE TABLE IF NOT EXISTS `ApiBetUserAccount` (
    `ApiBetId` int NOT NULL,
    `UserId` int NOT NULL,
    CONSTRAINT `PK_ApiBetUserAccount` PRIMARY KEY (`ApiBetId`, `UserId`),
    INDEX `IX_ApiBetUserAccount_UserId` (`UserId`),
    CONSTRAINT `FK_ApiBetUserAccount_ApiBet_ApiBetId` FOREIGN KEY (`ApiBetId`) REFERENCES `ApiBet` (`ApiBetId`) ON DELETE CASCADE,
    CONSTRAINT `FK_ApiBetUserAccount_UserAccount_UserId` FOREIGN KEY (`UserId`) REFERENCES `UserAccount` (`UserId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
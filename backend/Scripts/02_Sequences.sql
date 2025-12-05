-- ==================================================
-- DEÜ DUYURU YÖNETİM SİSTEMİ - ORACLE DATABASE
-- 02_SEQUENCES.sql - Sequence Tanımlamaları
-- ==================================================
-- Tüm tablolar için Oracle sequence'ları oluşturur
-- Primary key değerleri için kullanılır
-- ==================================================

PROMPT ========================================
PROMPT DEÜ Duyuru Sistemi - Sequences Oluşturuluyor
PROMPT ========================================

-- Ana sistem tabloları
CREATE SEQUENCE SEQ_ROLLER
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_KULLANICILAR
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_SISTEM_AYARLARI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Email grup yönetimi
CREATE SEQUENCE SEQ_EPOSTA_GRUPLARI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_EPOSTA_GRUP_UYELERI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Şablon kategori
CREATE SEQUENCE SEQ_SABLON_KATEGORILERI 
    START WITH 1 
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;    

-- Şablon sistemi
CREATE SEQUENCE SEQ_EPOSTA_SABLONLARI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Duyuru sistemi
CREATE SEQUENCE SEQ_EPOSTA_DUYURULARI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_EPOSTA_DUYURU_HAREKETLERI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_EPOSTA_DUYURU_ALICILARI
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_EPOSTA_DUYURU_ZAMANLAMALAR
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Gönderim log sistemi
CREATE SEQUENCE SEQ_DUYURU_GONDERIM_LOG
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Dosya yönetimi
CREATE SEQUENCE SEQ_DOSYALAR
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Log tabloları
CREATE SEQUENCE SEQ_LOG_LOGIN
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SEQ_LOG_SISTEM
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

COMMIT;

PROMPT ========================================
PROMPT Sequences başarıyla oluşturuldu
PROMPT ========================================

-- Kontrol sorgusu
PROMPT Oluşturulan sequences:
SELECT sequence_name, last_number 
FROM user_sequences 
WHERE sequence_name LIKE 'SEQ_%' 
ORDER BY sequence_name;

SELECT 'SEQUENCES script completed successfully' AS STATUS FROM DUAL;
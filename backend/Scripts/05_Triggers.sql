-- ==================================================
-- DEÜ DUYURU YÖNETİM SİSTEMİ - ORACLE DATABASE
-- 05_TRIGGERS_CRITICAL.sql - Kritik Sistem Trigger'ları
-- ==================================================
-- Sadece iş mantığı için kritik olan trigger'lar
-- Gereksiz GUNCELLEME_TARIHI trigger'ları kaldırıldı
-- ==================================================

PROMPT ========================================
PROMPT DEÜ Duyuru Sistemi - Kritik Triggers
PROMPT ========================================

-- =============================================
-- 1. EPOSTA DUYURULARI AUDIT TRIGGER (Kritik)
-- =============================================
PROMPT 1. EPOSTA_DUYURULARI audit trigger...

CREATE OR REPLACE TRIGGER TRG_DUYURULARI_AUDIT
    BEFORE UPDATE ON EPOSTA_DUYURULARI
    FOR EACH ROW
BEGIN
    -- Güncelleme tarihini otomatik set et
    :NEW.GUNCELLEME_TARIHI := SYSDATE;
END;
/

-- =============================================
-- 2. ALICI SAYISI OTOMATIK HESAPLAMA TRIGGER (Kritik)
-- =============================================
PROMPT 2. EPOSTA_DUYURU_ALICILARI alıcı sayısı hesaplama trigger...

-- Package: Mutating table sorununu çözmek için
CREATE OR REPLACE PACKAGE PKG_ALICI_SAYISI AS
    TYPE t_duyuru_ids IS TABLE OF NUMBER INDEX BY PLS_INTEGER;
    g_duyuru_ids t_duyuru_ids;
    g_index PLS_INTEGER := 0;

    PROCEDURE add_duyuru_id(p_duyuru_id NUMBER);
    PROCEDURE hesapla_ve_temizle;
END PKG_ALICI_SAYISI;
/

CREATE OR REPLACE PACKAGE BODY PKG_ALICI_SAYISI AS
    PROCEDURE add_duyuru_id(p_duyuru_id NUMBER) IS
    BEGIN
        g_index := g_index + 1;
        g_duyuru_ids(g_index) := p_duyuru_id;
    END;

    PROCEDURE hesapla_ve_temizle IS
        v_toplam_alici NUMBER := 0;
        v_grup_uye_sayisi NUMBER := 0;
    BEGIN
        -- Her unique duyuru için hesapla
        FOR i IN 1..g_index LOOP
            v_toplam_alici := 0;

            -- HYBRID APPROACH: Grup UYE_SAYISI değerini oku (backend tarafından güncellenmiş)
            -- Dynamic SQL kaldırıldı - performans ve güvenlik iyileştirmesi
            FOR alici IN (
                SELECT DISTINCT a.ID, a.GRUP_ID, a.EMAIL
                FROM EPOSTA_DUYURU_ALICILARI a
                WHERE a.DUYURU_ID = g_duyuru_ids(i)
            ) LOOP
                IF alici.GRUP_ID IS NOT NULL THEN
                    -- Grup alıcısı: EPOSTA_GRUPLARI.UYE_SAYISI değerini oku
                    -- Backend bu değeri güncel tutar (AddMember, RemoveMember, ImportMembers, CreateGroup)
                    BEGIN
                        SELECT UYE_SAYISI
                        INTO v_grup_uye_sayisi
                        FROM EPOSTA_GRUPLARI
                        WHERE ID = alici.GRUP_ID;

                        v_toplam_alici := v_toplam_alici + v_grup_uye_sayisi;
                    EXCEPTION
                        WHEN NO_DATA_FOUND THEN
                            -- Grup bulunamadı (silinmiş olabilir)
                            DBMS_OUTPUT.PUT_LINE('GRUP bulunamadı (ID=' || alici.GRUP_ID || ')');
                            v_toplam_alici := v_toplam_alici + 0;
                    END;
                ELSE
                    -- Manuel alıcı (grup yok): +1
                    v_toplam_alici := v_toplam_alici + 1;
                END IF;
            END LOOP;

            -- Duyuru tablosunu güncelle
            UPDATE EPOSTA_DUYURULARI
            SET TOPLAM_ALICI_SAYISI = v_toplam_alici,
                GUNCELLEME_TARIHI = SYSDATE
            WHERE ID = g_duyuru_ids(i);
        END LOOP;

        -- Temizle
        g_duyuru_ids.DELETE;
        g_index := 0;

    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('PKG_ALICI_SAYISI Error: ' || SQLERRM);
            g_duyuru_ids.DELETE;
            g_index := 0;
    END hesapla_ve_temizle;
END PKG_ALICI_SAYISI;
/

-- Row-level trigger: Sadece duyuru ID'yi topla
CREATE OR REPLACE TRIGGER TRG_ALICI_SAYISI_ROW
    AFTER INSERT OR DELETE ON EPOSTA_DUYURU_ALICILARI
    FOR EACH ROW
BEGIN
    PKG_ALICI_SAYISI.add_duyuru_id(COALESCE(:NEW.DUYURU_ID, :OLD.DUYURU_ID));
END;
/

-- Statement-level trigger: Hesapla ve güncelle
CREATE OR REPLACE TRIGGER TRG_ALICI_SAYISI_STMT
    AFTER INSERT OR DELETE ON EPOSTA_DUYURU_ALICILARI
BEGIN
    PKG_ALICI_SAYISI.hesapla_ve_temizle;
END;
/

-- =============================================
-- 3. GRUP UYE SAYISI OTOMATIK GÜNCELLEME - KALDIRILDI
-- =============================================
-- HYBRID APPROACH: Backend'de EmailGroupService.UpdateGroupMemberCountAsync() metodu kullanılıyor
-- Trigger'lar kaldırıldı - performans ve test edilebilirlik iyileştirmesi
PROMPT 3. EPOSTA_GRUP_UYELERI trigger'ları kaldırıldı (Backend hesaplıyor)...

-- =============================================
-- 4. SISTEM AYARLARI AUDIT TRIGGER - KALDIRILDI
-- =============================================
-- BACKEND APPROACH: SystemSettingsService.UpdateSettingsAsync() içinde audit log tutuluyor
-- Trigger'da kullanıcı ID ve IP adresi bilgisi alınamadığı için kaldırıldı
-- Backend audit log daha detaylı: KullaniciId, IP, User-Agent, tam değişiklik listesi
PROMPT 4. SISTEM_AYARLARI trigger kaldırıldı (Backend audit log kullanılıyor)...

COMMIT;

PROMPT ========================================
PROMPT Kritik trigger'lar oluşturuldu
PROMPT - TRG_DUYURULARI_AUDIT: AKTIF (Güncelleme tarihi)
PROMPT - TRG_ALICI_SAYISI: AKTIF (Duyuru alıcı sayısı - basitleştirilmiş)
PROMPT - TRG_GRUP_UYE_SAYISI: KALDIRILDI (Backend hesaplıyor)
PROMPT - TRG_SISTEM_AYARLARI_AUDIT: KALDIRILDI (Backend audit log kullanılıyor)
PROMPT ========================================
PROMPT HYBRID APPROACH:
PROMPT - EPOSTA_GRUPLARI.UYE_SAYISI -> Backend (EmailGroupService)
PROMPT - EPOSTA_DUYURULARI.TOPLAM_ALICI_SAYISI -> Trigger (basitleştirilmiş)
PROMPT - SISTEM_AYARLARI audit log -> Backend (SystemSettingsService)
PROMPT ========================================

-- Kontrol
PROMPT Aktif trigger'lar:
SELECT trigger_name, table_name, status, triggering_event
FROM user_triggers 
WHERE trigger_name LIKE 'TRG_%'
ORDER BY table_name;

SELECT 'CRITICAL TRIGGERS completed successfully' AS STATUS FROM DUAL;
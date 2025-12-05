public class DbProcess
{
    public DbProcess()
    {
    }

    /*
    public static Object KullaniciBul(string email)
    {
        List<Object> liste = new();

        DbConnection connObj = new();
        OracleCommand selectCommand = connObj.GetCommand();
        selectCommand.Parameters.Add("tcno", tcno);
        selectCommand.CommandText = @"
SELECT DISTINCT AD||' '||SOYAD AS ADSOYAD, TC_KIMLIK_NO, AKD_UNVAN, EMAIL, GOREV_YERI
FROM XXPER.V_AKDBASVURU_KULLANICI
WHERE ((:tcno <> 0 and TC_KIMLIK_NO = :tcno ) or (:tcno = 0)) ORDER BY 1,2";
        try
        {
            connObj.OpenConnection();
            OracleDataReader dr = selectCommand.ExecuteReader();
            while (dr.Read())
            {
                liste.Add(new
                {
                    AkdUnvan = dr.GetValue(dr.GetOrdinal("AKD_UNVAN")).ToString(),
                    AdSoyad = dr.GetValue(dr.GetOrdinal("ADSOYAD")).ToString(),
                    TckimliNo = dr.GetValue(dr.GetOrdinal("TC_KIMLIK_NO")).ToString(),
                    Email = dr.GetValue(dr.GetOrdinal("EMAIL")).ToString(),
                    GorevYeri = dr.GetValue(dr.GetOrdinal("GOREV_YERI")).ToString(),
                });
            }
            dr.Close();
            connObj.CloseConnection();
        }
        catch (Exception ex)
        {
            connObj.KillConnection();
            Console.WriteLine("---DBProcess-kullaniciListele: " + ex.Message);
            throw;
        }
        return liste;
    }
    */
}
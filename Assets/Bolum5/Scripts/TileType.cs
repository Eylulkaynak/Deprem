namespace DepremOyunu
{
    /// <summary>
    /// Bir karonun oyun içindeki rolü.
    /// Start/Goal/Decision/Open hepsi yürünebilir (safe); sadece Hazard engelli.
    /// </summary>
    public enum TileType
    {
        Open,       // güvenli, boş alan
        Start,      // başlangıç karosu
        Goal,       // toplanma alanı (bayrak/tabela)
        Hazard,     // tehlikeli karo - yürünemez
        Decision    // güvenli ama üzerine gelince karar baloncuğu açılır
    }
}

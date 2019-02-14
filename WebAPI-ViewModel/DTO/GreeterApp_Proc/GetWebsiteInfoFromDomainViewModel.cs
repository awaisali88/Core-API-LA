namespace WebAPI_ViewModel.DTO
{
    public class GetWebsiteInfoFromDomainViewModel
    {
        public long WebsiteId { get; set; }

        public int PriorityLanguage { get; set; }

        public string TimeZone { get; set; }

        public string DomainName { get; set; }
    }
}


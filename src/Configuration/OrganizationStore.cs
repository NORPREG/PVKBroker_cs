namespace HelseId.Configuration;

public static class OrganizationStore
{
    private static readonly List<Organization> Organizations = new()
    {
        new Organization
        {
            Id = 1,
            OrgNoParent = ConfigurationValues.OUSOrganizationNumber,
            ParentName = ConfigurationValues.OUSOrganizationName
        }
    };

    public static Organization? GetOrganization(int id)
    {
        return Organizations.SingleOrDefault(o =>
            o.Id == id);
    }

    public static Organization? GetOrganization(string? parentOrganizationNumber)
    {
        if (parentOrganizationNumber == null)
        {
            return null;
        }

        return Organizations.FirstOrDefault(o =>
            o.OrgNoParent == parentOrganizationNumber);
    }

    public static Organization? GetOrganizationWithChild(string? childOrganizationNumber)
    {
        if (childOrganizationNumber == null)
        {
            return null;
        }

        return Organizations.FirstOrDefault(
            o => o.OrgNoChild == childOrganizationNumber);
    }
}

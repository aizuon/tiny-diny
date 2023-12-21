namespace TinyDNS.Packets;

public record DNSResponse : IDeserializable<DNSResponse>
{
    public DNSHeader Header { get; set; }
    public DNSQuestion Question { get; set; }
    public DNSResourceRecord[] Answers { get; set; }
    public DNSResourceRecord[] Authorities { get; set; }
    public DNSResourceRecord[] Additionals { get; set; }

    public static DNSResponse Deserialize(BinaryBuffer buffer)
    {
        var response = new DNSResponse();

        var header = DNSHeader.Deserialize(buffer);
        if (header == null)
            return null;
        response.Header = header;

        var question = DNSQuestion.Deserialize(buffer);
        if (question == null)
            return null;
        response.Question = question;

        response.Answers = new DNSResourceRecord[header.AnswerRRs];
        for (int i = 0; i < header.AnswerRRs; i++)
        {
            var answer = DNSResourceRecord.Deserialize(buffer);
            if (answer == null)
                return null;
            response.Answers[i] = answer;
        }

        response.Authorities = new DNSResourceRecord[header.AuthorityRRs];
        for (int i = 0; i < header.AuthorityRRs; i++)
        {
            var authority = DNSResourceRecord.Deserialize(buffer);
            if (authority == null)
                return null;
            response.Authorities[i] = authority;
        }

        response.Additionals = new DNSResourceRecord[header.AdditionalRRs];
        for (int i = 0; i < header.AdditionalRRs; i++)
        {
            var additional = DNSResourceRecord.Deserialize(buffer);
            if (additional == null)
                return null;
            response.Additionals[i] = additional;
        }

        return response;
    }
}

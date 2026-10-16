namespace DatasetBuilder;

// A web shop's help centre: one row per article. search_text (question and answer together) is what
// gets searched: a customer's words often match the answer better than the question ("where is my
// parcel" is answered by the tracking article). The other columns come back with each match, so a
// chatbot or a search box can show them without a second lookup.
internal static class HelpCentreFaq
{
    private static readonly (string Id, string Question, string Answer, string Slug)[] Articles =
    [
        ("ship-01", "How long does delivery take?", "Standard delivery takes 3–5 working days. Express delivery arrives the next working day if you order before 2 pm.", "delivery-times"),
        ("ship-02", "Do you ship internationally?", "Yes, we ship to 34 countries in Europe and North America. Delivery takes 5–10 working days and duties are shown at checkout.", "international-shipping"),
        ("ship-03", "How much does shipping cost?", "Standard delivery is free on orders over €50, otherwise €4.95. Express delivery costs €9.95.", "shipping-costs"),
        ("ship-04", "How can I track my order?", "You get an email with a tracking link as soon as your parcel leaves our warehouse. You can also find it under Orders in your account.", "track-order"),
        ("ship-05", "My parcel says delivered but I haven't received it", "Check with neighbours and your building's reception first. If it hasn't turned up within 48 hours, contact us and we'll open an investigation with the courier.", "missing-parcel"),
        ("ship-06", "Can I change my delivery address after ordering?", "You can change the address within one hour of ordering from the order page. After that, contact us and we'll try to redirect the parcel.", "change-address"),
        ("ship-07", "Can I pick up my order from a parcel locker?", "Yes. Choose 'Parcel locker' at checkout and select a locker near you. You'll get a code by text message when it's ready.", "parcel-lockers"),
        ("ship-08", "My order arrived damaged", "We're sorry! Send us photos of the item and the box within 14 days and we'll send a replacement or a full refund.", "damaged-item"),
        ("ship-09", "Some items are missing from my order", "Large orders are sometimes split into several parcels; check your shipping email. If an item is still missing, contact us with your order number.", "missing-items"),
        ("ret-01", "What is your return policy?", "You can return any unused item within 30 days of delivery for a full refund. Sale items can be returned within 14 days.", "returns-policy"),
        ("ret-02", "How do I return an item?", "Start a return under Orders in your account, print the prepaid label, and drop the parcel at any post office or parcel locker.", "start-return"),
        ("ret-03", "Are returns free?", "Returns are free within the EU. For other countries the return label costs €7.50, deducted from your refund.", "return-costs"),
        ("ret-04", "When will I get my refund?", "Refunds are issued within 5 working days of your return reaching our warehouse. Your bank may take another 3–5 days to show it.", "refund-timing"),
        ("ret-05", "Can I exchange an item for a different size?", "Yes. Choose 'Exchange' when you start a return and pick the new size. We send it as soon as the original is scanned by the courier.", "exchanges"),
        ("ret-06", "Can I return a gift?", "Yes. Use the gift receipt code to start a return; the refund goes to the person who placed the order as a gift card.", "return-gift"),
        ("pay-01", "Which payment methods do you accept?", "We accept Visa, Mastercard, American Express, PayPal, Apple Pay, Google Pay and bank transfer.", "payment-methods"),
        ("pay-02", "Can I pay in instalments?", "Yes, orders over €100 can be split into 3 interest-free monthly payments with Klarna at checkout.", "instalments"),
        ("pay-03", "I was charged twice for my order", "A second charge is usually a temporary authorisation that disappears within 3–5 days. If it doesn't, send us a screenshot of your statement.", "double-charge"),
        ("pay-04", "How do I get an invoice for my order?", "Download a PDF invoice from the order page in your account. To add a company name or VAT number, edit your billing details before ordering.", "invoices"),
        ("pay-05", "My discount code doesn't work", "Codes are case-sensitive and can't be combined. Check the expiry date and the minimum order value in the email that sent you the code.", "discount-codes"),
        ("pay-06", "Is it safe to use my card on your site?", "Yes. Payments are processed by our payment provider over an encrypted connection; we never see or store your full card number.", "payment-security"),
        ("pay-07", "Do you offer gift cards?", "Yes, digital gift cards from €10 to €500, delivered by email and valid for 3 years.", "gift-cards"),
        ("acc-01", "How do I reset my password?", "Click 'Forgot password' on the sign-in page and we'll email you a link. The link is valid for 1 hour.", "reset-password"),
        ("acc-02", "I didn't receive the password reset email", "Check your spam folder and make sure you used the email address your account was created with. You can request a new link after 5 minutes.", "reset-email-missing"),
        ("acc-03", "How do I change my email address?", "Go to Account → Personal details, enter the new address and confirm it from the email we send there.", "change-email"),
        ("acc-04", "How do I delete my account?", "Go to Account → Privacy and choose 'Delete account'. Your data is erased within 30 days, except what we must keep for tax reasons.", "delete-account"),
        ("acc-05", "Can I order without creating an account?", "Yes, choose 'Guest checkout'. You can create an account later with the same email to see the order.", "guest-checkout"),
        ("acc-06", "How do I unsubscribe from your newsletter and marketing emails?", "Click 'Unsubscribe' at the bottom of any newsletter, or turn it off under Account → Notifications.", "unsubscribe"),
        ("acc-07", "My account is locked", "Accounts are locked for 30 minutes after 5 failed sign-in attempts. Wait, then reset your password if you've forgotten it.", "locked-account"),
        ("acc-08", "How do I turn on two-factor authentication?", "Go to Account → Security and choose 'Two-step sign-in'. You can use an authenticator app or text messages.", "two-factor"),
        ("ord-01", "Can I cancel my order?", "You can cancel within one hour of ordering from the order page. After that the order is being packed; return it once it arrives instead.", "cancel-order"),
        ("ord-02", "Can I add an item to an order I already placed?", "Orders can't be changed once placed. Place a second order; if both ship the same day we'll pack them together.", "add-to-order"),
        ("ord-03", "An item is out of stock. When will it be back?", "Click 'Notify me' on the product page and we'll email you as soon as it's back. Most items are restocked within 2–3 weeks.", "restock"),
        ("ord-04", "Do you have a physical store?", "We're online only, but you can collect orders from our warehouse in Rotterdam on weekdays from 9 am to 5 pm.", "collect"),
        ("ord-05", "Can I pre-order items?", "Yes, pre-order items are marked on the product page with their expected shipping date. You're charged when the item ships.", "pre-orders"),
        ("prod-01", "How do I find my size?", "Each product page has a size guide with measurements in centimetres and inches. If you're between sizes, we recommend the larger one.", "size-guide"),
        ("prod-02", "What is the warranty on electronics?", "All electronics come with a 2-year warranty. Keep your invoice, and contact us if the product stops working.", "warranty"),
        ("prod-03", "Are your products sustainable?", "Over 60% of our range carries an independent sustainability label. Filter by 'Sustainable' to see them.", "sustainability"),
        ("prod-04", "How do I wash my clothes?", "Washing instructions are on the label inside each garment and on the product page under 'Care'.", "care-instructions"),
        ("prod-05", "Do you sell refurbished products?", "Yes, our 'Like new' section has tested refurbished electronics with a 1-year warranty.", "refurbished"),
        ("biz-01", "Do you offer discounts for businesses?", "Yes, business accounts get volume pricing and invoice payment on 30-day terms. Apply on our business page.", "business-accounts"),
        ("biz-02", "Do you offer student discounts?", "Students get 10% off after verifying their status once with their student email address.", "student-discount"),
        ("biz-03", "Do you have a loyalty programme?", "Members earn 1 point per euro spent and get €5 off for every 250 points, plus early access to sales.", "loyalty"),
        ("help-01", "How can I contact customer service or talk to a person?", "Chat with us on the website from 8 am to 8 pm every day, or email support and we'll answer within 24 hours.", "contact"),
        ("help-02", "What are your customer service hours?", "Our team is available by chat from 8 am to 8 pm, seven days a week, except on public holidays.", "service-hours"),
        ("help-03", "How do I report a problem with the website?", "Use 'Report a problem' at the bottom of any page and describe what happened; a screenshot helps.", "report-problem"),
        ("help-04", "How do you use my personal data?", "We use your data only to process orders and, if you agree, to send offers. Read the full privacy policy for details.", "privacy"),
        ("help-05", "Do you use cookies?", "Yes, to keep you signed in and to measure how the site is used. You can change your choice under 'Cookie settings' at any time.", "cookies"),
    ];

    public static Task BuildAsync(string repoRoot)
    {
        var folder = RepoPaths.DataFolder(repoRoot, Path.Combine("text", "answer-questions-from-faq"));
        Csv.Write(Path.Combine(folder, "help-articles.csv"), ["id", "search_text", "question", "answer", "link"],
            Articles.Select(a => new object[]
            {
                a.Id, $"{a.Question} {a.Answer}", a.Question, a.Answer, $"https://shop.example.com/help/{a.Slug}"
            }));
        return Task.CompletedTask;
    }
}

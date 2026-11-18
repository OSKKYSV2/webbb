// Client side companion for the server-side VinAttribute.
if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
    jQuery.validator.addMethod('vin', function (value, element) {
        return this.optional(element) || /^[A-HJ-NPR-Z0-9]{17}$/i.test(value.trim());
    });
    jQuery.validator.unobtrusive.adapters.addBool('vin');
    jQuery.validator.unobtrusive.parse(document);
}

// Lucas Pedroso do Bomdespacho
// Formatos de data e número usados nos formulários
jQuery.extend(jQuery.validator.methods, {
    date: function (value, element) {
        // O input HTML de data envia yyyy-MM-dd; o padrão da aula usa dd/MM/yyyy.
        return this.optional(element) || /^\d\d?\/\d\d?\/\d\d\d?\d?$/.test(value)
            || /^\d{4}-\d{2}-\d{2}$/.test(value);
    },
    number: function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(value);
    }
});

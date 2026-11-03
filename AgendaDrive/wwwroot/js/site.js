// Campos decimais (inputmode="decimal"): se a pessoa digitar ou colar com ponto, por exemplo uma coordenada
// copiada do Google Maps ("-20.4237"), troca o ponto pela vírgula, que é o formato pt-BR que o servidor espera.
document.addEventListener('change', function (evento) {
    var campo = evento.target;
    if (campo.matches('input[inputmode="decimal"]') &&
        campo.value.indexOf(',') === -1 &&
        campo.value.split('.').length === 2) {
        campo.value = campo.value.replace('.', ',');
    }
});

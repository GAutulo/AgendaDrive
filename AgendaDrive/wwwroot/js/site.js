// Evita clique duplo: depois do primeiro envio, o botão do formulário fica desativado.
// O servidor também trata o envio repetido; isto só evita a segunda requisição.
document.addEventListener('submit', function (evento) {
    if (evento.defaultPrevented) return;
    var botao = evento.target.querySelector('button[type="submit"]');
    if (botao) setTimeout(function () { botao.disabled = true; }, 0);
});

// Ao voltar pelo botão "voltar" do navegador, a página vem do cache com o botão desativado.
window.addEventListener('pageshow', function (evento) {
    if (evento.persisted) {
        document.querySelectorAll('form button[type="submit"]:disabled:not(#enviar-corrida)')
            .forEach(function (botao) { botao.disabled = false; });
    }
});

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

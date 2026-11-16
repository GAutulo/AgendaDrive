// Mapa (Leaflet), sugestão de endereços e cotação. Tudo passa pela nossa API (/api/...):
// a chave do OpenRouteService fica só no servidor. O texto vindo da API entra na tela com
// textContent, nunca com innerHTML, para um endereço não conseguir injetar HTML.
(function () {
    'use strict';

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

    function criarMapa(idElemento) {
        var mapa = L.map(idElemento).setView([-20.4237, -49.9781], 12);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(mapa);
        return mapa;
    }

    function cotar(origem, destino, intermunicipal) {
        return fetch('/api/cotacoes', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token.value
            },
            body: JSON.stringify({ origem: origem, destino: destino, intermunicipal: !!intermunicipal })
        }).then(function (resposta) {
            if (!resposta.ok) throw new Error('cotacao');
            return resposta.json();
        });
    }

    function desenharRota(mapa, camada, origem, destino, rotaGeoJson) {
        camada.clearLayers();
        L.marker([origem.latitude, origem.longitude]).addTo(camada);
        L.marker([destino.latitude, destino.longitude]).addTo(camada);
        var linha = L.geoJSON(JSON.parse(rotaGeoJson), { style: { color: '#0B6E79', weight: 5, opacity: .9 } }).addTo(camada);
        mapa.fitBounds(linha.getBounds(), { padding: [24, 24] });
    }

    // Tela de detalhes: desenha a rota já gravada na corrida.
    function iniciarDetalhes(elemento) {
        var mapa = criarMapa(elemento.id);
        var camada = L.layerGroup().addTo(mapa);
        var origem = { latitude: Number(elemento.dataset.origemLat), longitude: Number(elemento.dataset.origemLng) };
        var destino = { latitude: Number(elemento.dataset.destinoLat), longitude: Number(elemento.dataset.destinoLng) };

        L.marker([origem.latitude, origem.longitude]).addTo(camada);
        L.marker([destino.latitude, destino.longitude]).addTo(camada);
        mapa.fitBounds([[origem.latitude, origem.longitude], [destino.latitude, destino.longitude]], { padding: [32, 32] });

        cotar(origem, destino)
            .then(function (cotacao) { desenharRota(mapa, camada, origem, destino, cotacao.rotaGeoJson); })
            .catch(function () { /* sem o serviço de rotas, os dois marcadores continuam no mapa */ });
    }

    // Tela de nova corrida.
    function iniciarNovaCorrida() {
        var mapa = criarMapa('mapa');
        var camada = L.layerGroup().addTo(mapa);
        var botao = document.getElementById('enviar-corrida');
        var resumo = document.getElementById('resumo-preco');
        var aviso = document.getElementById('aviso-cotacao');
        var ocupados = document.getElementById('ocupados');
        var dataHora = document.getElementById('DataHora');
        var intermunicipal = document.getElementById('Intermunicipal');

        // O formulário vai para o servidor em pt-BR, por isso a coordenada usa vírgula nos campos ocultos.
        function lerCoordenada(prefixo) {
            var latitude = document.getElementById(prefixo + 'Latitude').value;
            var longitude = document.getElementById(prefixo + 'Longitude').value;
            if (!latitude || !longitude) return null;
            return { latitude: Number(latitude.replace(',', '.')), longitude: Number(longitude.replace(',', '.')) };
        }

        function gravarCoordenada(prefixo, coordenada) {
            document.getElementById(prefixo + 'Latitude').value = coordenada ? String(coordenada.latitude).replace('.', ',') : '';
            document.getElementById(prefixo + 'Longitude').value = coordenada ? String(coordenada.longitude).replace('.', ',') : '';
        }

        function mostrarResumo(cotacao) {
            var linhas = [
                ['Tarifa-base', cotacao.valorBase],
                [cotacao.distanciaKm.toLocaleString('pt-BR') + ' km de corrida', cotacao.valorDistancia],
                [cotacao.duracaoMin + ' min de corrida', cotacao.valorTempo],
                [cotacao.deslocamentoKm.toLocaleString('pt-BR') + ' km de deslocamento', cotacao.valorDeslocamento]
            ];
            if (cotacao.valorRetorno > 0) {
                linhas.push([cotacao.retornoKm.toLocaleString('pt-BR') + ' km de retorno vazio (50%)', cotacao.valorRetorno]);
            }
            var tabela = document.createElement('table');
            tabela.className = 'table table-sm mb-0';
            var corpo = tabela.createTBody();

            linhas.forEach(function (linha) {
                var tr = corpo.insertRow();
                tr.insertCell().textContent = linha[0];
                var valor = tr.insertCell();
                valor.className = 'text-end';
                valor.textContent = moeda.format(linha[1]);
            });

            var total = corpo.insertRow();
            total.className = 'fw-semibold';
            total.insertCell().textContent = 'Preço estimado';
            var valorTotal = total.insertCell();
            valorTotal.className = 'text-end';
            valorTotal.textContent = moeda.format(cotacao.precoEstimado);

            resumo.replaceChildren(tabela);
            resumo.classList.remove('d-none');
        }

        function limparCotacao() {
            botao.disabled = true;
            resumo.classList.add('d-none');
            aviso.classList.add('d-none');
        }

        function atualizarCotacao() {
            var origem = lerCoordenada('Origem');
            var destino = lerCoordenada('Destino');
            limparCotacao();
            if (!origem || !destino) return;

            cotar(origem, destino, intermunicipal.checked)
                .then(function (cotacao) {
                    desenharRota(mapa, camada, origem, destino, cotacao.rotaGeoJson);
                    mostrarResumo(cotacao);
                    botao.disabled = false;
                })
                .catch(function () {
                    aviso.textContent = 'Não foi possível calcular a rota agora. Confira os endereços ou tente novamente.';
                    aviso.classList.remove('d-none');
                });
        }

        function iniciarAutocomplete(prefixo) {
            var campo = document.getElementById(prefixo);
            var lista = document.getElementById('sugestoes-' + prefixo);
            var espera = null;
            var requisicao = null;

            function fechar() { lista.replaceChildren(); }

            campo.addEventListener('input', function () {
                gravarCoordenada(prefixo, null);
                limparCotacao();
                clearTimeout(espera);

                var texto = campo.value.trim();
                if (texto.length < 3) { fechar(); return; }

                // Espera uma pausa na digitação para não gastar a cota gratuita a cada tecla (RNF11).
                espera = setTimeout(function () {
                    if (requisicao) requisicao.abort();
                    requisicao = new AbortController();

                    fetch('/api/enderecos?texto=' + encodeURIComponent(texto), { signal: requisicao.signal })
                        .then(function (resposta) { return resposta.ok ? resposta.json() : []; })
                        .then(function (enderecos) {
                            fechar();
                            enderecos.forEach(function (endereco) {
                                var item = document.createElement('button');
                                item.type = 'button';
                                item.className = 'list-group-item list-group-item-action';
                                item.textContent = endereco.descricao;
                                item.addEventListener('click', function () {
                                    campo.value = endereco.descricao;
                                    gravarCoordenada(prefixo, endereco.coordenada);
                                    fechar();
                                    atualizarCotacao();
                                });
                                lista.appendChild(item);
                            });
                        })
                        .catch(function () { /* requisição cancelada por uma digitação mais nova */ });
                }, 350);
            });

            document.addEventListener('click', function (evento) {
                if (evento.target !== campo) fechar();
            });
        }

        // Mostra só início e fim dos períodos ocupados do dia escolhido (RF05, RN11).
        function atualizarOcupados() {
            var data = dataHora.value.substring(0, 10);
            ocupados.textContent = '';
            if (!data) return;

            fetch('/api/agenda/disponibilidade?data=' + data)
                .then(function (resposta) { return resposta.ok ? resposta.json() : []; })
                .then(function (periodos) {
                    if (periodos.length === 0) {
                        ocupados.textContent = 'Nenhum horário ocupado neste dia.';
                        return;
                    }
                    var hora = { hour: '2-digit', minute: '2-digit' };
                    ocupados.textContent = 'Ocupado neste dia: ' + periodos.map(function (p) {
                        return new Date(p.inicio).toLocaleTimeString('pt-BR', hora) + ' às ' +
                            new Date(p.fim).toLocaleTimeString('pt-BR', hora);
                    }).join(', ') + '.';
                });
        }

        iniciarAutocomplete('Origem');
        iniciarAutocomplete('Destino');
        dataHora.addEventListener('change', atualizarOcupados);
        intermunicipal.addEventListener('change', atualizarCotacao);

        // Depois de um erro de validação, a página volta com os endereços preenchidos.
        atualizarCotacao();
        atualizarOcupados();
    }

    var detalhes = document.getElementById('mapa-corrida');
    if (detalhes) iniciarDetalhes(detalhes);
    if (document.getElementById('form-corrida')) iniciarNovaCorrida();
})();

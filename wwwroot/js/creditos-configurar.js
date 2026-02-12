(function () {
    document.addEventListener('DOMContentLoaded', () => {
        const form = document.querySelector('form[data-total-venta]');
        if (!form) return;

        const anticipoInput = document.getElementById('anticipoInput');
        const cuotasInput = document.getElementById('cuotasInput');
        const gastosInput = document.getElementById('GastosAdministrativos');
        const fechaInput = document.getElementById('fechaPrimeraCuota');
        const metodoCalculoSelect = document.getElementById('metodoCalculoSelect'); // TAREA 9
        const perfilSelectorDiv = document.getElementById('perfilSelectorDiv'); // TAREA 9
        const perfilCreditoSelect = document.getElementById('perfilCreditoSelect'); // TAREA 9
        const tasaMensualInput = document.getElementById('tasaMensualInput');
        const tasaBadge = document.getElementById('tasaBadge');
        const metodoHelpText = document.getElementById('metodoHelpText'); // TAREA 9
        const tasaHelpText = document.getElementById('tasaHelpText');
        const cuotasMinLabel = document.getElementById('cuotasMinLabel'); // TAREA 9
        const cuotasMaxLabel = document.getElementById('cuotasMaxLabel');
        const configData = document.getElementById('configData');
        const fuenteConfigHidden = document.getElementById('fuenteConfigHidden'); // TAREA 9: hidden field
        const btnGuardarCredito = document.getElementById('btnGuardarCredito'); // PUNTO 2
        const avisoMetodoRequerido = document.getElementById('avisoMetodoRequerido'); // PUNTO 2

        const alertaError = document.getElementById('errorCalculadora');
        const mensajeSemaforo = document.getElementById('mensajeSemaforo');
        const badgeSemaforo = document.getElementById('estadoSemaforo');
        const msgIngreso = document.getElementById('msgIngreso');
        const msgAntiguedad = document.getElementById('msgAntiguedad');
        const interesTotalLabel = document.getElementById('interesTotalLabel');
        const totalAPagarLabel = document.getElementById('totalAPagarLabel');
        const tasaAplicadaLabel = document.getElementById('tasaAplicadaLabel');
        const capitalFinanciadoLabel = document.getElementById('capitalFinanciadoLabel');
        const gastosAdministrativosLabel = document.getElementById('gastosAdministrativosLabel');
        const planTotalLabel = document.getElementById('planTotalLabel');
        const fechaPrimerPagoLabel = document.getElementById('fechaPrimerPagoLabel');
        const montoFinanciadoInput = document.getElementById('MontoFinanciado');
        const montoFinanciadoLabel = document.getElementById('montoFinanciadoLabel');
        const cuotaEstimadaLabel = document.getElementById('cuotaEstimadaLabel');

        if (!anticipoInput || !cuotasInput || !gastosInput || !fechaInput) return;

        const totalVenta = parseFloat(form.dataset.totalVenta) || 0;

        // TAREA 9: Cargar datos de configuración desde data attributes
        const tasaGlobal = parseFloat(configData?.dataset.tasaGlobal) || 0;
        const gastosGlobales = parseFloat(configData?.dataset.gastosGlobales) || 0;
        const tasaCliente = parseFloat(configData?.dataset.tasaCliente) || null;
        const gastosCliente = parseFloat(configData?.dataset.gastosCliente) || null;
        const cuotasMaxCliente = parseInt(configData?.dataset.cuotasMaxCliente, 10) || 24;
        const cuotasMinCliente = parseInt(configData?.dataset.cuotasMinCliente, 10) || 1;
        const tieneConfigCliente = configData?.dataset.tieneConfigCliente === 'true';
        const tienePerfilPreferido = configData?.dataset.tienePerfilPreferido === 'true';
        const perfilPreferidoId = configData?.dataset.perfilPreferidoId ? parseInt(configData.dataset.perfilPreferidoId, 10) : null;
        const perfilTasa = parseFloat(configData?.dataset.perfilTasa) || null;
        const perfilGastos = parseFloat(configData?.dataset.perfilGastos) || null;
        const perfilMinCuotas = parseInt(configData?.dataset.perfilMinCuotas, 10) || 1;
        const perfilMaxCuotas = parseInt(configData?.dataset.perfilMaxCuotas, 10) || 24;

        // TAREA 9.2: Rastrear cambios manuales del operador
        let valoresIniciales = {
            tasa: null,
            gastos: null,
            cuotas: null
        };
        let camposModificadosManualmente = {
            tasa: false,
            gastos: false,
            cuotas: false
        };

        function formatear(valor) {
            return valor.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }

        // TAREA 9.2: Guardar valores iniciales para detectar cambios manuales
        function guardarValoresIniciales() {
            valoresIniciales.tasa = parseFloat(tasaMensualInput?.value) || 0;
            valoresIniciales.gastos = parseFloat(gastosInput?.value) || 0;
            valoresIniciales.cuotas = parseInt(cuotasInput?.value, 10) || 0;
        }

        // TAREA 9.2: Verificar si hubo modificaciones manuales
        function hayModificacionesManuales() {
            const tasaActual = parseFloat(tasaMensualInput?.value) || 0;
            const gastosActuales = parseFloat(gastosInput?.value) || 0;
            const cuotasActuales = parseInt(cuotasInput?.value, 10) || 0;

            const tasaCambiada = Math.abs(tasaActual - valoresIniciales.tasa) > 0.01;
            const gastosCambiados = Math.abs(gastosActuales - valoresIniciales.gastos) > 0.01;
            const cuotasCambiadas = cuotasActuales !== valoresIniciales.cuotas;

            return tasaCambiada || gastosCambiados || cuotasCambiadas;
        }

        // TAREA 9.2: Mostrar banner de aviso cuando se actualizan valores
        // PUNTO 3: Mejorado para mostrar valores precargados
        function mostrarBannerActualizacion(metodoNombre, tasa, gastos, cuotasMin, cuotasMax) {
            // Buscar contenedor para el banner (antes del formulario)
            const cardBody = form.closest('.card-body');
            if (!cardBody) return;

            // Remover banner anterior si existe
            const bannerAnterior = document.getElementById('bannerActualizacionMetodo');
            if (bannerAnterior) {
                bannerAnterior.remove();
            }

            // Crear nuevo banner con valores detallados
            const banner = document.createElement('div');
            banner.id = 'bannerActualizacionMetodo';
            banner.className = 'alert alert-info alert-dismissible fade show d-flex align-items-center gap-2 mb-3';
            banner.setAttribute('role', 'alert');
            banner.innerHTML = `
                <i class="bi bi-info-circle-fill"></i>
                <div>
                    <strong>✓ Valores precargados del método "${metodoNombre}":</strong><br>
                    <small>
                        • Tasa: ${tasa.toFixed(2)}% mensual
                        • Gastos: $${gastos.toFixed(2)}
                        • Cuotas: ${cuotasMin} - ${cuotasMax}
                    </small>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;

            // Insertar banner al inicio del card-body
            cardBody.insertBefore(banner, cardBody.firstChild);

            // Auto-ocultar después de 5 segundos
            setTimeout(() => {
                if (banner && banner.parentNode) {
                    banner.classList.remove('show');
                    setTimeout(() => banner.remove(), 300);
                }
            }, 5000);
        }

        // PUNTO 3: Mostrar banner de advertencia cuando se sobrescriben valores manuales
        function mostrarBannerSobrescritura() {
            const cardBody = form.closest('.card-body');
            if (!cardBody) return;

            // Remover banner anterior si existe
            const bannerAnterior = document.getElementById('bannerSobrescritura');
            if (bannerAnterior) {
                bannerAnterior.remove();
            }

            // Crear banner de advertencia
            const banner = document.createElement('div');
            banner.id = 'bannerSobrescritura';
            banner.className = 'alert alert-warning alert-dismissible fade show d-flex align-items-center gap-2 mb-3';
            banner.setAttribute('role', 'alert');
            banner.innerHTML = `
                <i class="bi bi-exclamation-triangle-fill"></i>
                <div>
                    <strong>⚠️ Valores sobrescritos:</strong> Los cambios manuales anteriores fueron reemplazados por los valores del nuevo método.
                    Si desea usar valores personalizados, seleccione el método "Manual".
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;

            // Insertar banner al inicio del card-body
            cardBody.insertBefore(banner, cardBody.firstChild);

            // Auto-ocultar después de 8 segundos (más tiempo por ser advertencia)
            setTimeout(() => {
                if (banner && banner.parentNode) {
                    banner.classList.remove('show');
                    setTimeout(() => banner.remove(), 300);
                }
            }, 8000);
        }

        // TAREA 9: Actualizar configuración según método de cálculo seleccionado
        function actualizarMetodoCalculo(forzarActualizacion = false) {
            const metodo = parseInt(metodoCalculoSelect?.value, 10);
            
            // PUNTO 1: Si no hay selección válida, deshabilitar campos y mostrar mensaje
            if (isNaN(metodo) || metodo === -1 || metodoCalculoSelect?.value === '') {
                // Deshabilitar campos hasta que se seleccione método
                if (tasaMensualInput) {
                    tasaMensualInput.disabled = true;
                    tasaMensualInput.value = '';
                    tasaMensualInput.placeholder = 'Seleccione método primero';
                }
                if (gastosInput) {
                    gastosInput.disabled = true;
                    gastosInput.value = '';
                }
                if (cuotasInput) {
                    cuotasInput.disabled = true;
                    cuotasInput.value = '';
                }
                if (tasaBadge) {
                    tasaBadge.textContent = 'Sin método';
                    tasaBadge.className = 'badge bg-secondary';
                }
                if (metodoHelpText) {
                    metodoHelpText.textContent = '⚠️ Debe seleccionar un método de cálculo para continuar.';
                }
                if (perfilSelectorDiv) {
                    perfilSelectorDiv.style.display = 'none';
                }
                // PUNTO 2: Mostrar aviso y deshabilitar botón guardar
                if (avisoMetodoRequerido) {
                    avisoMetodoRequerido.classList.remove('d-none');
                }
                if (btnGuardarCredito) {
                    btnGuardarCredito.disabled = true;
                }
                limpiarResultados();
                return;
            }
            
            // PUNTO 2: Ocultar aviso y habilitar botón guardar una vez que hay método
            if (avisoMetodoRequerido) {
                avisoMetodoRequerido.classList.add('d-none');
            }
            if (btnGuardarCredito) {
                btnGuardarCredito.disabled = false;
            }
            
            // Habilitar campos una vez que hay método seleccionado
            if (cuotasInput) {
                cuotasInput.disabled = false;
            }

            // TAREA 9.2: Verificar si hay modificaciones manuales y el operador está cambiando método
            if (!forzarActualizacion && hayModificacionesManuales()) {
                const confirmar = confirm(
                    '⚠️ Has modificado valores manualmente.\n\n' +
                    'Al cambiar el método de cálculo se sobrescribirán:\n' +
                    '• Tasa mensual\n' +
                    '• Gastos administrativos\n' +
                    '• Rango de cuotas\n\n' +
                    '¿Deseas continuar?'
                );

                if (!confirmar) {
                    // Revertir selección del método (mantener el anterior)
                    metodoCalculoSelect.value = metodoCalculoSelect.dataset.metodoAnterior || '0';
                    return;
                }
                
                // PUNTO 3: Si confirma, mostrar banner de advertencia adicional
                mostrarBannerSobrescritura();
            }

            // Guardar método actual para próxima comparación
            metodoCalculoSelect.dataset.metodoAnterior = metodo;

            let configuracion = {
                badge: 'Sin definir',
                badgeClass: 'bg-secondary',
                helpText: 'Selecciona un método de cálculo',
                tasaHelp: '',
                readonly: true,
                tasa: tasaGlobal,
                gastos: gastosGlobales,
                cuotasMin: 1,
                cuotasMax: 24,
                mostrarPerfilSelector: false,
                fuenteEquivalente: 0, // Global
                nombreMetodo: 'Sin definir'
            };

            switch (metodo) {
                case 0: // AutomaticoPorCliente
                    // Prioridad: Cliente personalizado > Perfil preferido > Global
                    if (tieneConfigCliente) {
                        configuracion = {
                            badge: 'Auto (Cliente)',
                            badgeClass: 'bg-success',
                            helpText: 'Usando configuración personalizada del cliente',
                            tasaHelp: 'Tasa específica del cliente',
                            readonly: true,
                            tasa: tasaCliente || tasaGlobal,
                            gastos: gastosCliente || gastosGlobales,
                            cuotasMin: cuotasMinCliente,
                            cuotasMax: cuotasMaxCliente,
                            mostrarPerfilSelector: false,
                            fuenteEquivalente: 1, // PorCliente
                            nombreMetodo: 'Automático (Por Cliente)'
                        };
                    } else if (tienePerfilPreferido) {
                        configuracion = {
                            badge: 'Auto (Perfil)',
                            badgeClass: 'bg-info text-dark',
                            helpText: 'Usando perfil preferido del cliente',
                            tasaHelp: 'Tasa del perfil preferido',
                            readonly: true,
                            tasa: perfilTasa || tasaGlobal,
                            gastos: perfilGastos || gastosGlobales,
                            cuotasMin: perfilMinCuotas,
                            cuotasMax: perfilMaxCuotas,
                            mostrarPerfilSelector: false,
                            fuenteEquivalente: 1, // PorCliente
                            nombreMetodo: 'Automático (Perfil Preferido)'
                        };
                    } else {
                        configuracion = {
                            badge: 'Auto (Global)',
                            badgeClass: 'bg-info text-dark',
                            helpText: 'Usando valores globales del sistema',
                            tasaHelp: 'Tasa configurada en el sistema',
                            readonly: true,
                            tasa: tasaGlobal,
                            gastos: gastosGlobales,
                            cuotasMin: 1,
                            cuotasMax: 24,
                            mostrarPerfilSelector: false,
                            fuenteEquivalente: 0, // Global
                            nombreMetodo: 'Automático (Global)'
                        };
                    }
                    break;

                case 1: // UsarPerfil
                    configuracion.badge = 'Perfil';
                    configuracion.badgeClass = 'bg-primary';
                    configuracion.helpText = 'Selecciona un perfil de crédito';
                    configuracion.tasaHelp = 'Tasa del perfil seleccionado';
                    configuracion.readonly = true;
                    configuracion.mostrarPerfilSelector = true;
                    configuracion.fuenteEquivalente = 3; // PorPlan
                    configuracion.nombreMetodo = 'Usar Perfil';
                    
                    // Si hay perfil seleccionado, cargar sus valores
                    const perfilSeleccionado = perfilCreditoSelect?.selectedOptions[0];
                    if (perfilSeleccionado && perfilSeleccionado.value) {
                        configuracion.tasa = parseFloat(perfilSeleccionado.dataset.tasa) || tasaGlobal;
                        configuracion.gastos = parseFloat(perfilSeleccionado.dataset.gastos) || gastosGlobales;
                        configuracion.cuotasMin = parseInt(perfilSeleccionado.dataset.minCuotas, 10) || 1;
                        configuracion.cuotasMax = parseInt(perfilSeleccionado.dataset.maxCuotas, 10) || 24;
                        configuracion.nombreMetodo = `Perfil: ${perfilSeleccionado.textContent}`;
                    }
                    break;

                case 2: // UsarCliente
                    if (!tieneConfigCliente) {
                        // TAREA 10 / PUNTO 4: NO permitir usar este método si el cliente no tiene config
                        alert('❌ Error: El cliente no tiene configuración de crédito personal.\n\n' +
                              'Para usar este método debe configurar al cliente con:\n' +
                              '• Tasa de interés personalizada, o\n' +
                              '• Gastos administrativos personalizados, o\n' +
                              '• Cuotas máximas personalizadas\n\n' +
                              'Por favor configure el cliente o seleccione otro método.');
                        metodoCalculoSelect.value = '3'; // Forzar cambio a Global
                        actualizarMetodoCalculo(true);
                        return;
                    }
                    configuracion = {
                        badge: 'Cliente',
                        badgeClass: 'bg-success',
                        helpText: 'Valores personalizados del cliente',
                        tasaHelp: 'Tasa específica para este cliente',
                        readonly: true,
                        tasa: tasaCliente || tasaGlobal,
                        gastos: gastosCliente || gastosGlobales,
                        cuotasMin: cuotasMinCliente,
                        cuotasMax: cuotasMaxCliente,
                        mostrarPerfilSelector: false,
                        fuenteEquivalente: 1, // PorCliente
                        nombreMetodo: 'Usar Cliente'
                    };
                    break;

                case 3: // Global
                    configuracion = {
                        badge: 'Global',
                        badgeClass: 'bg-info text-dark',
                        helpText: 'Valores del sistema (configuración global)',
                        tasaHelp: 'Tasa configurada en el sistema',
                        readonly: true,
                        tasa: tasaGlobal,
                        gastos: gastosGlobales,
                        cuotasMin: 1,
                        cuotasMax: 24,
                        mostrarPerfilSelector: false,
                        fuenteEquivalente: 0, // Global
                        nombreMetodo: 'Global'
                    };
                    break;

                case 4: // Manual
                    configuracion = {
                        badge: 'Manual',
                        badgeClass: 'bg-warning text-dark',
                        helpText: 'Ingresa valores personalizados para esta venta',
                        tasaHelp: 'Edita la tasa manualmente para este crédito',
                        readonly: false,
                        tasa: tasaCliente || perfilTasa || tasaGlobal, // Base inicial
                        gastos: gastosCliente || perfilGastos || gastosGlobales,
                        cuotasMin: 1,
                        cuotasMax: 120, // Sin restricción estricta en manual
                        mostrarPerfilSelector: false,
                        fuenteEquivalente: 2, // Manual
                        nombreMetodo: 'Manual'
                    };
                    break;
            }

            // Actualizar hidden field de FuenteConfiguracion para compatibilidad
            if (fuenteConfigHidden) {
                fuenteConfigHidden.value = configuracion.fuenteEquivalente;
            }

            // Mostrar/ocultar selector de perfil
            if (perfilSelectorDiv) {
                perfilSelectorDiv.style.display = configuracion.mostrarPerfilSelector ? 'block' : 'none';
            }

            // Actualizar badge
            if (tasaBadge) {
                tasaBadge.textContent = configuracion.badge;
                tasaBadge.className = `badge ${configuracion.badgeClass}`;
            }

            // Actualizar help texts
            if (metodoHelpText) {
                metodoHelpText.textContent = configuracion.helpText;
            }
            if (tasaHelpText) {
                tasaHelpText.textContent = configuracion.tasaHelp;
            }

            // Actualizar campo de tasa
            if (tasaMensualInput) {
                tasaMensualInput.disabled = false; // Habilitar campo
                tasaMensualInput.value = configuracion.tasa.toFixed(2);
                tasaMensualInput.readOnly = configuracion.readonly;
                
                if (configuracion.readonly) {
                    tasaMensualInput.classList.add('bg-body-secondary');
                    tasaMensualInput.classList.remove('bg-dark');
                } else {
                    tasaMensualInput.classList.remove('bg-body-secondary');
                    tasaMensualInput.classList.add('bg-dark');
                }
            }

            // Actualizar gastos administrativos
            if (gastosInput) {
                gastosInput.disabled = false; // Habilitar campo
                gastosInput.value = configuracion.gastos.toFixed(2);
                // En modo manual, permitir edición de gastos
                gastosInput.readOnly = configuracion.readonly;
                if (configuracion.readonly) {
                    gastosInput.classList.add('bg-body-secondary');
                    gastosInput.classList.remove('bg-dark');
                } else {
                    gastosInput.classList.remove('bg-body-secondary');
                    gastosInput.classList.add('bg-dark');
                }
            }

            // Actualizar rango de cuotas
            if (cuotasMinLabel) {
                cuotasMinLabel.textContent = configuracion.cuotasMin;
            }
            if (cuotasMaxLabel) {
                cuotasMaxLabel.textContent = configuracion.cuotasMax;
            }
            if (cuotasInput) {
                cuotasInput.min = configuracion.cuotasMin;
                cuotasInput.max = configuracion.cuotasMax;
                
                // Ajustar cuotas actuales si están fuera del rango
                const cuotasActuales = parseInt(cuotasInput.value, 10) || configuracion.cuotasMin;
                if (cuotasActuales < configuracion.cuotasMin) {
                    cuotasInput.value = configuracion.cuotasMin;
                } else if (cuotasActuales > configuracion.cuotasMax) {
                    cuotasInput.value = configuracion.cuotasMax;
                }
            }

            // TAREA 9.2: Guardar nuevos valores iniciales para próximas comparaciones
            guardarValoresIniciales();

            // TAREA 9.2: Mostrar banner informativo de actualización
            // PUNTO 3: Pasar valores precargados para mostrar en el banner
            if (!forzarActualizacion) {
                mostrarBannerActualizacion(configuracion.nombreMetodo, configuracion.tasa, configuracion.gastos, configuracion.cuotasMin, configuracion.cuotasMax);
            }

            // Recalcular después de cambiar el método
            actualizarCalculos();
        }

        function normalizarCamposIniciales() {
            if (anticipoInput.value === '0') anticipoInput.value = '';
            if (cuotasInput.value === '0') cuotasInput.value = '';
            if (gastosInput.value === '0') gastosInput.value = '';
        }

        function limpiarResultados() {
            if (cuotaEstimadaLabel) cuotaEstimadaLabel.innerText = '$0,00';
            if (interesTotalLabel) interesTotalLabel.innerText = '$0,00';
            if (totalAPagarLabel) totalAPagarLabel.innerText = '$0,00';
            if (tasaAplicadaLabel) tasaAplicadaLabel.innerText = '0%';
            if (capitalFinanciadoLabel) capitalFinanciadoLabel.innerText = '$0,00';
            if (gastosAdministrativosLabel) gastosAdministrativosLabel.innerText = '$0,00';
            if (planTotalLabel) planTotalLabel.innerText = '$0,00';
            if (fechaPrimerPagoLabel) fechaPrimerPagoLabel.innerText = '--';
            if (mensajeSemaforo) mensajeSemaforo.innerText = 'Completa los datos para precalificar.';
            if (badgeSemaforo) {
                badgeSemaforo.className = 'badge state-yellow';
                badgeSemaforo.innerText = 'Sin datos';
            }
            if (msgIngreso) msgIngreso.classList.add('d-none');
            if (msgAntiguedad) msgAntiguedad.classList.add('d-none');
        }

        function actualizarSemaforo(estado, mensaje, mostrarIngreso, mostrarAntiguedad) {
            const clases = {
                verde: 'badge state-green',
                amarillo: 'badge state-yellow',
                rojo: 'badge state-red',
                sinDatos: 'badge state-yellow',
            };

            const etiquetas = {
                verde: 'Verde',
                amarillo: 'Amarillo',
                rojo: 'Rojo',
                sinDatos: 'Sin datos',
            };

            if (badgeSemaforo) {
                badgeSemaforo.className = clases[estado] || 'badge state-yellow';
                badgeSemaforo.innerText = etiquetas[estado] || 'Sin datos';
            }

            if (mensajeSemaforo) {
                mensajeSemaforo.innerText = mensaje || 'Completa los datos para precalificar.';
            }

            if (msgIngreso) {
                msgIngreso.classList.toggle('d-none', !mostrarIngreso);
            }

            if (msgAntiguedad) {
                msgAntiguedad.classList.toggle('d-none', !mostrarAntiguedad);
            }
        }

        async function actualizarCalculos() {
            const anticipo = parseFloat(anticipoInput.value) || 0;
            const cuotas = parseInt(cuotasInput.value, 10) || 0;
            const gastos = parseFloat(gastosInput.value) || 0;
            const fecha = fechaInput.value;
            const tasaMensualActual = parseFloat(tasaMensualInput?.value) || 0;

            // TAREA 10: Validar rangos de cuotas según método activo
            const metodo = parseInt(metodoCalculoSelect?.value, 10);
            let cuotasMin = 1;
            let cuotasMax = 120;
            
            switch (metodo) {
                case 0: // AutomaticoPorCliente
                    if (tienePerfilPreferido) {
                        cuotasMin = perfilMinCuotas;
                        cuotasMax = perfilMaxCuotas;
                    } else {
                        cuotasMin = 1;
                        cuotasMax = 24;
                    }
                    break;
                case 1: // UsarPerfil
                    const perfilSeleccionado = perfilCreditoSelect?.selectedOptions[0];
                    if (perfilSeleccionado && perfilSeleccionado.value) {
                        cuotasMin = parseInt(perfilSeleccionado.dataset.minCuotas, 10) || 1;
                        cuotasMax = parseInt(perfilSeleccionado.dataset.maxCuotas, 10) || 24;
                    }
                    break;
                case 2: // UsarCliente
                    cuotasMin = cuotasMinCliente;
                    cuotasMax = cuotasMaxCliente;
                    break;
                case 3: // Global
                    cuotasMin = 1;
                    cuotasMax = 24;
                    break;
                case 4: // Manual
                    cuotasMin = 1;
                    cuotasMax = 120;
                    break;
            }
            
            // Validar rango de cuotas
            if (cuotas < cuotasMin || cuotas > cuotasMax) {
                cuotasInput.classList.add('is-invalid');
                if (alertaError) {
                    alertaError.innerText = `La cantidad de cuotas debe estar entre ${cuotasMin} y ${cuotasMax} según el método seleccionado.`;
                    alertaError.classList.remove('d-none');
                }
                limpiarResultados();
                return;
            } else {
                cuotasInput.classList.remove('is-invalid');
            }
            
            // TAREA 10: Validar tasa > 0 en Manual
            if (metodo === 4 && tasaMensualActual <= 0) {
                if (tasaMensualInput) tasaMensualInput.classList.add('is-invalid');
                if (alertaError) {
                    alertaError.innerText = 'La tasa de interés debe ser mayor a 0% en modo Manual.';
                    alertaError.classList.remove('d-none');
                }
                limpiarResultados();
                return;
            } else {
                if (tasaMensualInput) tasaMensualInput.classList.remove('is-invalid');
            }

            const datosCompletos = cuotas > 0 && totalVenta > 0;

            if (!datosCompletos) {
                limpiarResultados();
                if (alertaError) alertaError.classList.remove('d-none');
                return;
            }

            if (alertaError) alertaError.classList.add('d-none');

            try {
                const url = `/Credito/SimularPlanVenta?totalVenta=${encodeURIComponent(totalVenta)}&anticipo=${encodeURIComponent(anticipo)}&cuotas=${encodeURIComponent(cuotas)}&gastosAdministrativos=${encodeURIComponent(gastos)}&fechaPrimeraCuota=${encodeURIComponent(fecha)}&tasaMensual=${encodeURIComponent(tasaMensualActual)}`;
                const response = await fetch(url, { headers: { 'Accept': 'application/json' } });

                if (!response.ok) {
                    throw new Error('No se pudo calcular el plan de crédito.');
                }

                const data = await response.json();

                if (montoFinanciadoInput) {
                    montoFinanciadoInput.value = data.montoFinanciado;
                }
                if (montoFinanciadoLabel) {
                    montoFinanciadoLabel.innerText = `$${formatear(data.montoFinanciado || 0)}`;
                }
                if (cuotaEstimadaLabel) {
                    cuotaEstimadaLabel.innerText = `$${formatear(data.cuotaEstimada || 0)}`;
                }
                if (tasaAplicadaLabel) {
                    const tasaAplicada = data.tasaAplicada || 0;
                    tasaAplicadaLabel.innerText = `${tasaAplicada.toFixed(2)}%`;
                }
                if (interesTotalLabel) {
                    interesTotalLabel.innerText = `$${formatear(data.interesTotal || 0)}`;
                }
                if (totalAPagarLabel) {
                    totalAPagarLabel.innerText = `$${formatear(data.totalAPagar || 0)}`;
                }
                if (capitalFinanciadoLabel) {
                    capitalFinanciadoLabel.innerText = `$${formatear(data.montoFinanciado || 0)}`;
                }
                if (gastosAdministrativosLabel) {
                    gastosAdministrativosLabel.innerText = `$${formatear(data.gastosAdministrativos || 0)}`;
                }
                if (planTotalLabel) {
                    planTotalLabel.innerText = `$${formatear(data.totalPlan || 0)}`;
                }
                if (fechaPrimerPagoLabel) {
                    fechaPrimerPagoLabel.innerText = data.fechaPrimerPago
                        ? new Date(data.fechaPrimerPago).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' })
                        : '--';
                }

                actualizarSemaforo(
                    data.semaforoEstado || 'sinDatos',
                    data.semaforoMensaje,
                    data.mostrarMsgIngreso,
                    data.mostrarMsgAntiguedad
                );
            } catch (error) {
                limpiarResultados();
                if (alertaError) {
                    alertaError.classList.remove('d-none');
                    alertaError.textContent = error.message;
                }
            }
        }

        normalizarCamposIniciales();
        
        // TAREA 9: Event listener para cambio de método de cálculo
        if (metodoCalculoSelect) {
            metodoCalculoSelect.addEventListener('change', actualizarMetodoCalculo);
            // Inicializar con el método seleccionado (forzar para no mostrar banner inicial)
            actualizarMetodoCalculo(true);
        }

        // TAREA 9: Event listener para cambio de perfil seleccionado
        if (perfilCreditoSelect) {
            perfilCreditoSelect.addEventListener('change', actualizarMetodoCalculo);
        }
        
        anticipoInput.addEventListener('input', actualizarCalculos);
        cuotasInput.addEventListener('input', actualizarCalculos);
        gastosInput.addEventListener('input', actualizarCalculos);
        fechaInput.addEventListener('change', actualizarCalculos);
        
        // TAREA 9.2: Event listener para tasa mensual (detectar cambios manuales)
        if (tasaMensualInput) {
            tasaMensualInput.addEventListener('input', () => {
                camposModificadosManualmente.tasa = true;
                actualizarCalculos();
            });
        }

        // TAREA 9.2: Event listener para gastos (detectar cambios manuales)
        if (gastosInput) {
            gastosInput.addEventListener('input', () => {
                camposModificadosManualmente.gastos = true;
            });
        }

        // TAREA 9.2: Event listener para cuotas (detectar cambios manuales)
        if (cuotasInput) {
            cuotasInput.addEventListener('input', () => {
                camposModificadosManualmente.cuotas = true;
            });
        }

        // TAREA 10: Validación antes de submit del formulario
        form.addEventListener('submit', (event) => {
            const metodo = parseInt(metodoCalculoSelect?.value, 10);
            const cuotas = parseInt(cuotasInput.value, 10) || 0;
            const tasaMensualActual = parseFloat(tasaMensualInput?.value) || 0;
            const fecha = fechaInput.value;
            
            let errores = [];
            
            // Validar fecha primera cuota (siempre obligatoria)
            if (!fecha) {
                errores.push('La fecha de primera cuota es obligatoria.');
                fechaInput.classList.add('is-invalid');
            } else {
                fechaInput.classList.remove('is-invalid');
            }
            
            // Validar tasa > 0 en Manual
            if (metodo === 4 && tasaMensualActual <= 0) {
                errores.push('La tasa de interés debe ser mayor a 0% en modo Manual.');
                if (tasaMensualInput) tasaMensualInput.classList.add('is-invalid');
            }
            
            // Validar rango de cuotas según método
            let cuotasMin = 1, cuotasMax = 120;
            let descripcionMetodo = '';
            
            switch (metodo) {
                case 0: // AutomaticoPorCliente
                    if (tienePerfilPreferido) {
                        cuotasMin = perfilMinCuotas;
                        cuotasMax = perfilMaxCuotas;
                        descripcionMetodo = 'Automático (Perfil)';
                    } else {
                        cuotasMin = 1;
                        cuotasMax = 24;
                        descripcionMetodo = 'Automático (Global)';
                    }
                    break;
                case 1: // UsarPerfil
                    const perfilSeleccionado = perfilCreditoSelect?.selectedOptions[0];
                    if (perfilSeleccionado && perfilSeleccionado.value) {
                        cuotasMin = parseInt(perfilSeleccionado.dataset.minCuotas, 10) || 1;
                        cuotasMax = parseInt(perfilSeleccionado.dataset.maxCuotas, 10) || 24;
                        descripcionMetodo = `Perfil: ${perfilSeleccionado.textContent}`;
                    } else {
                        errores.push('Debe seleccionar un perfil de crédito.');
                    }
                    break;
                case 2: // UsarCliente
                    if (!tieneConfigCliente) {
                        errores.push('El cliente no tiene configuración de crédito personal.');
                    }
                    cuotasMin = cuotasMinCliente;
                    cuotasMax = cuotasMaxCliente;
                    descripcionMetodo = 'Cliente';
                    break;
                case 3: // Global
                    cuotasMin = 1;
                    cuotasMax = 24;
                    descripcionMetodo = 'Global';
                    break;
                case 4: // Manual
                    cuotasMin = 1;
                    cuotasMax = 120;
                    descripcionMetodo = 'Manual';
                    break;
            }
            
            if (cuotas < cuotasMin || cuotas > cuotasMax) {
                errores.push(`La cantidad de cuotas debe estar entre ${cuotasMin} y ${cuotasMax} (método ${descripcionMetodo}).`);
                cuotasInput.classList.add('is-invalid');
            } else {
                cuotasInput.classList.remove('is-invalid');
            }
            
            // Mostrar errores y prevenir submit
            if (errores.length > 0) {
                event.preventDefault();
                event.stopPropagation();
                
                if (alertaError) {
                    alertaError.innerHTML = '<strong>❌ Errores de validación:</strong><ul class="mb-0 mt-2">' +
                        errores.map(err => `<li>${err}</li>`).join('') +
                        '</ul>';
                    alertaError.classList.remove('d-none');
                }
                
                // Scroll hacia el error
                if (alertaError) {
                    alertaError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                
                return false;
            }
            
            return true;
        });

        // PUNTO 2: Verificar estado inicial del método al cargar página
        actualizarMetodoCalculo(true); // forzar para no mostrar confirmación en carga inicial
        actualizarCalculos();
    });
})();

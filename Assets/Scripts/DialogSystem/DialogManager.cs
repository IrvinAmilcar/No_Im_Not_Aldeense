using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Colocamos o sistema dentro de um namespace para organizar melhor o código
namespace DialogSystem
{
    public class DialogManager : MonoBehaviour
    {
        // --- O Padrão Singleton ---
        public static DialogManager Instance { get; private set; }

        // --- Componentes da UI (criados dinamicamente) ---
        private CanvasGroup messageCanvasGroup;
        private Text messageText;
        
        // Referência da Corrotina (para podermos parar uma mensagem anterior)
        private Coroutine currentMessageCoroutine;
        
        // --- Configurações (pode mover para o Inspector se preferir) ---
        private float fadeSpeed = 4f; // Velocidade do fade in/out da caixa
        private Color panelColor = new Color(0f, 0f, 0f, 0.75f); // Cor do painel
        private int panelFontSize = 20;
        private Vector2 panelSize = new Vector2(650f, 60f); // Tamanho do painel
        private float panelBottomOffset = 50f; // Distância da base da tela

        // ======================= MUDANÇA IMPORTANTE =======================
        [Header("Configurações da UI (Assignável)")]
        [Tooltip("Arraste a fonte que você quer usar (ex: Arial) para cá. Se deixar nulo, tentará usar o 'Arial' do sistema.")]
        public Font messageFont;
        // ==================================================================

        void Awake()
        {
            // Configura o Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cria a UI que usaremos para as mensagens
            CreateMessageUI();
        }

        /// <summary>
        /// Cria dinamicamente os objetos de UI necessários para as mensagens.
        /// Segue o mesmo padrão do seu CameraFader.
        /// </summary>
        void CreateMessageUI()
        {
            // 1. O Objeto Canvas principal
            GameObject canvasObject = new GameObject("DialogMessageCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998; // Logo abaixo do fader (999)
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            // 2. O Painel de fundo (preto e transparente)
            GameObject panelObject = new GameObject("MessagePanel");
            panelObject.transform.SetParent(canvasObject.transform);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            
            // Configura o painel (ficará no centro inferior)
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f); // Pivô na base
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = new Vector2(0, panelBottomOffset);

            panelObject.AddComponent<Image>().color = panelColor;

            // Adiciona o CanvasGroup para controlar o fade
            messageCanvasGroup = panelObject.AddComponent<CanvasGroup>();
            messageCanvasGroup.alpha = 0f; // Começa invisível

            // 3. O Texto da mensagem
            GameObject textObject = new GameObject("MessageText");
            textObject.transform.SetParent(panelObject.transform);
            messageText = textObject.AddComponent<Text>();
            
            // ======================= MUDANÇA IMPORTANTE =======================
            // --- NOVA LÓGICA DA FONTE ---
            if (messageFont != null)
            {
                // Usa a fonte assignada no inspector (preferencial)
                messageText.font = messageFont;
            }
            else
            {
                // Se nada foi assignado, tenta usar a fonte padrão do OS (Arial).
                Debug.LogWarning("Nenhuma fonte foi assignada no 'Message Font' do DialogManager. Tentando usar 'Arial' do sistema.");
                try
                {
                    // Isso é mais confiável que o Resources.GetBuiltinResource.
                    messageText.font = Font.CreateDynamicFontFromOSFont("Arial", panelFontSize);
                }
                catch
                {
                    // Se tudo falhar, loga um erro claro.
                    Debug.LogError("FALHA AO CARREGAR FONTE! Por favor, arraste uma fonte (Arial) para o slot 'Message Font' no DialogManager.");
                }
            }
            // --- FIM DA NOVA LÓGICA ---
            // ==================================================================
            
            messageText.fontSize = panelFontSize;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.raycastTarget = false;

            // Faz o texto preencher o painel com padding
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 10); // Padding (left, bottom)
            textRect.offsetMax = new Vector2(-15, -10); // Padding (right, top)
        }

        /// <summary>
        /// Exibe uma mensagem na tela por um tempo determinado.
        /// </summary>
        /// <param name="message">O texto a ser exibido.</param>
        /// <param name="duration">Quantos segundos a mensagem ficará visível (antes de começar o fade out).</param>
        public void ShowMessage(string message, float duration)
        {
            // Se uma mensagem já estiver sendo mostrada, pare a corrotina antiga
            if (currentMessageCoroutine != null)
            {
                StopCoroutine(currentMessageCoroutine);
            }
            
            // Inicia a nova corrotina da mensagem
            messageText.text = message;
            currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(duration));
        }

        private IEnumerator ShowMessageCoroutine(float duration)
        {
            // 1. Fade In
            yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 1f, fadeSpeed));

            // 2. Espera
            yield return new WaitForSeconds(duration);

            // 3. Fade Out
            yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 0f, fadeSpeed));

            currentMessageCoroutine = null; // Limpa a referência
        }
        
        /// <summary>
        /// Corrotina auxiliar para dar fade em um CanvasGroup.
        /// </summary>
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float speed)
        {
            float startAlpha = cg.alpha;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            cg.alpha = targetAlpha;
        }
    }
}
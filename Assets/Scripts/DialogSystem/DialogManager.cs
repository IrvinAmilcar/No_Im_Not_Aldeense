using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; 

namespace DialogSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance { get; private set; }

        [Header("Configurações Gerais")]
        public Font messageFont; 
        private float fadeSpeed = 4f; 

        // --- Sistema 1: Mensagem Simples (Arma) ---
        [Header("Config. Mensagem Simples")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private int panelFontSize = 20;
        [SerializeField] private Vector2 panelSize = new Vector2(650f, 60f);
        [SerializeField] private float panelBottomOffset = 50f;
        
        private CanvasGroup messageCanvasGroup;
        private Text messageText;
        private Coroutine currentMessageCoroutine;

        // --- Sistema 2: Diálogo do Rádio ---
        [Header("Config. Diálogo do Rádio")]
        [SerializeField] private Color radioBackgroundColor = new Color(0f, 0f, 0f, 0.85f);
        
        // --- (CORREÇÃO DE POSIÇÃO) ---
        // O offset padrão agora é 0f, para colar na base.
        [Tooltip("Distância da base da tela. 0 = colado na base.")]
        [SerializeField] private float radioImageBottomOffset = 0f; 
        
        [Tooltip("Tamanho (Largura, Altura) da imagem do rádio na tela.")]
        [SerializeField] private Vector2 radioImageSize = new Vector2(800f, 300f); 

        // --- (NOVO: CONFIGURAÇÃO DOS TEXTOS) ---
        [Header("Config. Textos do Rádio")]
        [SerializeField] private float radioTextWidth = 800f;
        [SerializeField] private int radioDialogueFontSize = 22;
        [SerializeField] private int radioPromptFontSize = 18;
        [SerializeField] private string radioPromptMessage = "Pressione [ESPAÇO] para guardar";
        
        private CanvasGroup radioCanvasGroup;
        private Image radioImageComponent;
        private AudioSource radioAudioSource;
        
        // --- (NOVO: REFERÊNCIAS DE TEXTO) ---
        private Text radioDialogueText; // O diálogo principal
        private Text radioPromptText;   // O aviso "Pressione [ESPAÇO]"
        
        private bool isRadioDialogActive = false;
        private System.Action onRadioDialogComplete; 

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateMessageUI();
            CreateRadioDialogUI();

            radioAudioSource = GetComponent<AudioSource>();
            radioAudioSource.playOnAwake = false;
            radioAudioSource.loop = false;
            radioAudioSource.spatialBlend = 0f; 
        }

        void Update()
        {
            if (!isRadioDialogActive) return;

            // Se o áudio JÁ TERMINOU de tocar...
            if (!radioAudioSource.isPlaying)
            {
                // ...e o prompt de "guardar" AINDA NÃO está visível...
                if (!radioPromptText.enabled)
                {
                    // ...mostre o prompt!
                    radioPromptText.enabled = true;
                }
            }
            
            // Se o prompt está visível E o jogador apertar Espaço
            if (radioPromptText.enabled && Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(HideRadioDialogCoroutine());
            }
        }

        #region Sistema 1: Mensagem Simples (Arma)
        // ... (Esta seção está 100% igual, sem mudanças) ...
        void CreateMessageUI()
        {
            GameObject canvasObject = new GameObject("DialogMessageCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998; 
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            GameObject panelObject = new GameObject("MessagePanel");
            panelObject.transform.SetParent(canvasObject.transform);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = new Vector2(0, panelBottomOffset);
            panelObject.AddComponent<Image>().color = panelColor;
            messageCanvasGroup = panelObject.AddComponent<CanvasGroup>();
            messageCanvasGroup.alpha = 0f;
            GameObject textObject = new GameObject("MessageText");
            textObject.transform.SetParent(panelObject.transform);
            messageText = textObject.AddComponent<Text>();
            messageText.font = GetDefaultFont();
            messageText.fontSize = panelFontSize;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.raycastTarget = false;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 10);
            textRect.offsetMax = new Vector2(-15, -10);
        }

        public void ShowMessage(string message, float duration)
        {
            if (currentMessageCoroutine != null) StopCoroutine(currentMessageCoroutine);
            messageText.text = message;
            currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(duration));
        }

        private IEnumerator ShowMessageCoroutine(float duration)
        {
            yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 1f, fadeSpeed));
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 0f, fadeSpeed));
            currentMessageCoroutine = null;
        }
        #endregion

        // ===================================================================
        // --- LÓGICA DO SISTEMA 2 (Diálogo do Rádio) ---
        // (Grandes mudanças aqui)
        // ===================================================================

        void CreateRadioDialogUI()
        {
            GameObject canvasObject = new GameObject("RadioDialogCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 997; 
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            radioCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            radioCanvasGroup.alpha = 0f; 

            // 1. Fundo Preto
            GameObject bgObject = new GameObject("BackgroundScrim");
            bgObject.transform.SetParent(canvasObject.transform, false);
            RectTransform bgRect = bgObject.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgObject.AddComponent<Image>().color = radioBackgroundColor;

            // 2. Imagem das Mãos
            GameObject imageObject = new GameObject("RadioImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            radioImageComponent = imageObject.AddComponent<Image>();
            radioImageComponent.preserveAspect = true;
            RectTransform imageRect = radioImageComponent.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0f); // Base-Centro
            imageRect.anchorMax = new Vector2(0.5f, 0f); // Base-Centro
            imageRect.pivot = new Vector2(0.5f, 0f); // Pivô na Base
            imageRect.anchoredPosition = new Vector2(0, radioImageBottomOffset); // Posição (0)
            imageRect.sizeDelta = radioImageSize; // Tamanho (do Inspector)

            // --- (NOVO: OBJETO DE TEXTO DO DIÁLOGO) ---
            GameObject dialogueTextObject = new GameObject("RadioDialogueText");
            dialogueTextObject.transform.SetParent(canvasObject.transform, false);
            radioDialogueText = dialogueTextObject.AddComponent<Text>();
            radioDialogueText.font = GetDefaultFont();
            radioDialogueText.fontSize = radioDialogueFontSize;
            radioDialogueText.color = Color.white;
            radioDialogueText.alignment = TextAnchor.LowerCenter; // Alinha na base
            radioDialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            radioDialogueText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform dialogueRect = radioDialogueText.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0.5f, 0f); // Base-Centro
            dialogueRect.anchorMax = new Vector2(0.5f, 0f); // Base-Centro
            dialogueRect.pivot = new Vector2(0.5f, 0f); // Pivô na base
            // Posiciona acima da imagem (altura da imagem + offset + 20px de padding)
            float dialogueYPos = radioImageBottomOffset + radioImageSize.y + 20f;
            dialogueRect.anchoredPosition = new Vector2(0, dialogueYPos);
            dialogueRect.sizeDelta = new Vector2(radioTextWidth, 150f); // Largura (do Inspector) e Altura

            // --- (NOVO: OBJETO DE TEXTO DO PROMPT) ---
            GameObject promptTextObject = new GameObject("RadioPromptText");
            promptTextObject.transform.SetParent(canvasObject.transform, false);
            radioPromptText = promptTextObject.AddComponent<Text>();
            radioPromptText.font = GetDefaultFont();
            radioPromptText.fontSize = radioPromptFontSize;
            radioPromptText.color = Color.grey; // Cinza para ser sutil
            radioPromptText.alignment = TextAnchor.LowerCenter;
            radioPromptText.text = radioPromptMessage; // Texto fixo
            radioPromptText.enabled = false; // Começa invisível!
            RectTransform promptRect = radioPromptText.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f); // Base-Centro
            promptRect.anchorMax = new Vector2(0.5f, 0f); // Base-Centro
            promptRect.pivot = new Vector2(0.5f, 0f); // Pivô na base
            // Posiciona logo acima da imagem (entre a imagem e o diálogo)
            float promptYPos = radioImageBottomOffset + radioImageSize.y + 5f;
            promptRect.anchoredPosition = new Vector2(0, promptYPos);
            promptRect.sizeDelta = new Vector2(radioTextWidth, 40f);
        }

        // --- (MUDANÇA: AGORA RECEBE O TEXTO) ---
        public void ShowRadioDialog(string dialogueText, Sprite radioSprite, AudioClip dialogueClip, System.Action onCompleteCallback)
        {
            if (isRadioDialogActive) return;

            isRadioDialogActive = true;
            this.onRadioDialogComplete = onCompleteCallback;

            // 1. Configura a UI
            radioImageComponent.sprite = radioSprite;
            radioDialogueText.text = dialogueText; // Define o texto do diálogo
            radioPromptText.enabled = false;       // Esconde o prompt

            // 2. Toca o áudio
            if (dialogueClip != null)
            {
                radioAudioSource.PlayOneShot(dialogueClip);
            }
            else
            {
                // Se não houver áudio, mostre o prompt imediatamente
                radioPromptText.enabled = true;
            }

            // 3. Mostra o Canvas (Fade In)
            StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 1f, fadeSpeed));
        }

        private IEnumerator HideRadioDialogCoroutine()
        {
            isRadioDialogActive = false;
            
            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 0f, fadeSpeed));

            // Limpa o callback
            if (onRadioDialogComplete != null)
            {
                onRadioDialogComplete.Invoke();
            }
            onRadioDialogComplete = null; 
        }

        #region Funções Auxiliares
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

        private Font GetDefaultFont()
        {
            if (messageFont != null) return messageFont;
            Debug.LogWarning("Nenhuma fonte foi assignada no 'Message Font' do DialogManager. Usando 'Arial' do sistema.");
            try { return Font.CreateDynamicFontFromOSFont("Arial", 14); }
            catch { Debug.LogError("FALHA AO CARREGAR FONTE! Por favor, arraste uma fonte (Arial) para o slot 'Message Font' no DialogManager."); return null; }
        }
        #endregion
    }
}
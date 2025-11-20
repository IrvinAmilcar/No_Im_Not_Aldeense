using UnityEngine;
using UnityEngine.UI; // Importante para a UI
using System.Collections;
using System.Collections.Generic;

namespace DialogSystem
{
    // NÃO REQUER MAIS O AUDIOSOURCE
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
        [SerializeField] private float radioImageBottomOffset = 0f;
        [SerializeField] private Vector2 radioImageSize = new Vector2(800f, 300f);

        // REMOVEMOS O [SerializeField] private AudioSource radioAudioSource; 

        [Header("Config. Textos do Rádio")]
        [SerializeField] private float radioTextWidth = 800f;
        [SerializeField] private int radioDialogueFontSize = 22;
        [SerializeField] private int radioPromptFontSize = 18;

        [SerializeField] private string radioContinueMessage = "Pressione [ESPAÇO] para continuar...";
        [SerializeField] private string radioCloseMessage = "Pressione [ESPAÇO] para guardar";

        private CanvasGroup radioCanvasGroup;
        private Image radioImageComponent;
        // private AudioSource radioAudioSource; // REMOVIDO
        private Text radioDialogueText;
        private Text radioPromptText;

        private Coroutine currentRadioCoroutine;

        // --- Sistema 3: Diálogo de Peek (Janela) ---
        // (NENHUMA MUDANÇA AQUI)
        [Header("Config. Diálogo de Peek (Janela)")]
        [SerializeField] private Color peekPanelColor = new Color(0f, 0f, 0f, 0.75f);
        [Tooltip("Tamanho da caixa de diálogo principal")]
        [SerializeField] private Vector2 peekPanelSize = new Vector2(700f, 60f); // Caixa menor
        [Tooltip("Distância do fundo da tela para a CAIXA DE DIÁLOGO")]
        [SerializeField] private float peekPanelBottomOffset = 50f; // Posição da caixa
        [Tooltip("Distância do fundo da tela para o TEXTO DE PROMPT")]
        [SerializeField] private float peekPromptBottomOffset = 20f; // Posição do prompt (abaixo da caixa)
        [SerializeField] private int peekDialogueFontSize = 18;
        [SerializeField] private int peekPromptFontSize = 14;
        [SerializeField] private string peekContinueMessage = "Pressione [ESPAÇO] para continuar...";
        [SerializeField] private string peekCloseMessage = "Pressione [ESPAÇO] para voltar";

        private CanvasGroup peekCanvasGroup;
        private Text peekDialogueText;
        private Text peekPromptText;
        private Coroutine currentPeekCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cria as UIs para os 3 sistemas
            CreateMessageUI();
            CreateRadioDialogUI();
            CreatePeekDialogueUI();

            // REMOVEMOS TODA A CONFIGURAÇÃO DE AUDIOSOURCE DAQUI
        }

        #region Sistema 1: Mensagem Simples (Arma)

        // (NENHUMA MUDANÇA AQUI)
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
            // --- CORREÇÃO DE CLICK: Desliga Raycast quando invisível ---
            messageCanvasGroup.blocksRaycasts = false;

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


        #region Sistema 2: Diálogo do Rádio

        // (NENHUMA MUDANÇA AQUI)
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
            // --- CORREÇÃO DE CLICK: Desliga Raycast quando invisível ---
            radioCanvasGroup.blocksRaycasts = false;

            GameObject bgObject = new GameObject("BackgroundScrim");
            bgObject.transform.SetParent(canvasObject.transform, false);
            RectTransform bgRect = bgObject.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgObject.AddComponent<Image>().color = radioBackgroundColor;
            GameObject imageObject = new GameObject("RadioImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            radioImageComponent = imageObject.AddComponent<Image>();
            radioImageComponent.preserveAspect = true;
            RectTransform imageRect = radioImageComponent.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0f);
            imageRect.anchorMax = new Vector2(0.5f, 0f);
            imageRect.pivot = new Vector2(0.5f, 0f);
            imageRect.anchoredPosition = new Vector2(0, radioImageBottomOffset);
            imageRect.sizeDelta = radioImageSize;

            GameObject dialogueTextObject = new GameObject("RadioDialogueText");
            dialogueTextObject.transform.SetParent(canvasObject.transform, false);
            radioDialogueText = dialogueTextObject.AddComponent<Text>();
            radioDialogueText.font = GetDefaultFont();
            radioDialogueText.fontSize = radioDialogueFontSize;
            radioDialogueText.color = Color.white;
            radioDialogueText.alignment = TextAnchor.LowerCenter;
            radioDialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            radioDialogueText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform dialogueRect = radioDialogueText.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0.5f, 0f);
            dialogueRect.anchorMax = new Vector2(0.5f, 0f);
            dialogueRect.pivot = new Vector2(0.5f, 0f);

            GameObject promptTextObject = new GameObject("RadioPromptText");
            promptTextObject.transform.SetParent(canvasObject.transform, false);
            radioPromptText = promptTextObject.AddComponent<Text>();
            radioPromptText.font = GetDefaultFont();
            radioPromptText.fontSize = radioPromptFontSize;
            radioPromptText.color = Color.grey;
            radioPromptText.alignment = TextAnchor.LowerCenter;
            radioPromptText.enabled = false;
            RectTransform promptRect = promptTextObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);

            float promptYPos = radioImageBottomOffset + radioImageSize.y + 10f;
            promptRect.anchoredPosition = new Vector2(0, promptYPos);
            promptRect.sizeDelta = new Vector2(radioTextWidth, 40f);

            float dialogueYPos = promptYPos + 30f;
            dialogueRect.anchoredPosition = new Vector2(0, dialogueYPos);
            dialogueRect.sizeDelta = new Vector2(radioTextWidth, 150f);
        }

        // --- MUDANÇA: REMOVIDO O PARÂMETRO 'AudioClip dialogueClip' ---
        public void ShowRadioDialog(string[] dialoguePages, Sprite radioSprite, System.Action onCompleteCallback)
        {
            if (currentRadioCoroutine != null)
            {
                StopCoroutine(currentRadioCoroutine);
            }
            // --- MUDANÇA: NÃO PASSAMOS MAIS O 'dialogueClip' ---
            currentRadioCoroutine = StartCoroutine(ShowRadioDialogCoroutine(dialoguePages, radioSprite, onCompleteCallback));
        }

        // --- MUDANÇA: REMOVIDO O PARÂMETRO 'AudioClip clip' ---
        private IEnumerator ShowRadioDialogCoroutine(string[] pages, Sprite sprite, System.Action callback)
        {
            radioImageComponent.sprite = sprite;
            radioPromptText.enabled = false;
            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 1f, fadeSpeed));

            // --- MUDANÇA: REMOVIDO O BLOCO QUE TOCA O ÁUDIO ---
            // if (clip != null) ...

            if (pages != null && pages.Length > 0)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    radioDialogueText.text = pages[i];
                    bool isLastPage = (i == pages.Length - 1);

                    if (!isLastPage)
                    {
                        radioPromptText.text = radioContinueMessage;
                        radioPromptText.enabled = true;
                    }
                    else
                    {
                        radioPromptText.text = radioCloseMessage;
                        radioPromptText.enabled = true;
                    }

                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                    yield return null;
                }
            }

            // --- MUDANÇA: REMOVIDO O BLOCO QUE PARA O ÁUDIO ---
            // if (radioAudioSource.isPlaying) ...

            yield return StartCoroutine(FadeCanvasGroup(radioCanvasGroup, 0f, fadeSpeed));

            if (callback != null)
            {
                callback.Invoke();
            }
            currentRadioCoroutine = null;
        }
        #endregion


        #region Sistema 3: Diálogo de Peek (Janela)

        // (NENHUMA MUDANÇA AQUI)
        void CreatePeekDialogueUI()
        {
            // 1. Criar o Canvas
            GameObject canvasObject = new GameObject("PeekDialogueCanvas");
            canvasObject.transform.SetParent(this.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 996;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            peekCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            peekCanvasGroup.alpha = 0f;
            // --- CORREÇÃO DE CLICK: Desliga Raycast quando invisível ---
            peekCanvasGroup.blocksRaycasts = false;

            // 2. Criar o Painel (a "caixa preta") - SÓ PARA O DIÁLOGO
            GameObject panelObject = new GameObject("PeekPanel");
            panelObject.transform.SetParent(canvasObject.transform); // Filho do Canvas
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = peekPanelSize; // (ex: 700, 60)
            panelRect.anchoredPosition = new Vector2(0, peekPanelBottomOffset); // (ex: 50f)
            panelObject.AddComponent<Image>().color = peekPanelColor;

            // 3. Criar o Texto do Diálogo (DENTRO do painel)
            GameObject dialogueObject = new GameObject("PeekDialogueText");
            dialogueObject.transform.SetParent(panelObject.transform); // Filho do Painel
            peekDialogueText = dialogueObject.AddComponent<Text>();
            peekDialogueText.font = GetDefaultFont();
            peekDialogueText.fontSize = peekDialogueFontSize;
            peekDialogueText.color = Color.white;
            peekDialogueText.alignment = TextAnchor.MiddleCenter; // Centralizado na caixa
            peekDialogueText.raycastTarget = false;

            RectTransform dialogueRect = dialogueObject.GetComponent<RectTransform>();
            dialogueRect.anchorMin = Vector2.zero; // Preenche o painel
            dialogueRect.anchorMax = Vector2.one;
            dialogueRect.offsetMin = new Vector2(15, 10); // Padding
            dialogueRect.offsetMax = new Vector2(-15, -10); // Padding

            // 4. Criar o Texto do Prompt (FORA do painel, sem fundo)
            GameObject promptObject = new GameObject("PeekPromptText");
            promptObject.transform.SetParent(canvasObject.transform); // Filho do Canvas
            peekPromptText = promptObject.AddComponent<Text>();
            peekPromptText.font = GetDefaultFont();
            peekPromptText.fontSize = peekPromptFontSize;
            peekPromptText.color = Color.grey;
            peekPromptText.alignment = TextAnchor.LowerCenter;
            peekPromptText.raycastTarget = false;

            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.sizeDelta = new Vector2(peekPanelSize.x, 30f); // Mesma largura, 30px altura
            promptRect.anchoredPosition = new Vector2(0, peekPromptBottomOffset); // (ex: 20f)
        }

        public void ShowPeekDialogue(string[] dialoguePages, System.Action onCompleteCallback)
        {
            if (currentPeekCoroutine != null)
            {
                StopCoroutine(currentPeekCoroutine);
            }
            currentPeekCoroutine = StartCoroutine(ShowPeekDialogueCoroutine(dialoguePages, onCompleteCallback));
        }

        private IEnumerator ShowPeekDialogueCoroutine(string[] pages, System.Action callback)
        {
            // Limpa o texto antigo ANTES de mostrar o painel (Correção do Flicker)
            peekDialogueText.text = "";
            peekPromptText.text = "";
            peekPromptText.enabled = false;

            // 1. Ligar a UI (agora limpa)
            yield return StartCoroutine(FadeCanvasGroup(peekCanvasGroup, 1f, fadeSpeed));

            // 2. Loop pelas Páginas
            if (pages != null && pages.Length > 0)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    peekDialogueText.text = pages[i];
                    bool isLastPage = (i == pages.Length - 1);

                    if (!isLastPage)
                    {
                        peekPromptText.text = peekContinueMessage;
                        peekPromptText.enabled = true;
                    }
                    else
                    {
                        peekPromptText.text = peekCloseMessage;
                        peekPromptText.enabled = true;
                    }

                    // Espera o Espaço
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                    yield return null; // Espera 1 frame para evitar input duplicado
                }
            }

            // 3. Desligar a UI
            yield return StartCoroutine(FadeCanvasGroup(peekCanvasGroup, 0f, fadeSpeed));

            // 4. Chamar o Callback (avisar a janela para "voltar")
            if (callback != null)
            {
                callback.Invoke();
            }
            currentPeekCoroutine = null;
        }

        #endregion


        #region Funções Auxiliares

        // --- CORREÇÃO DE CLICK: Gerencia o blocksRaycasts durante o Fade ---
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float speed)
        {
            // Se vamos mostrar, ativamos o bloqueio imediatamente
            if (targetAlpha > 0f) cg.blocksRaycasts = true;

            float startAlpha = cg.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            cg.alpha = targetAlpha;

            // Se escondemos totalmente, desativamos o bloqueio para liberar cliques
            if (targetAlpha == 0f) cg.blocksRaycasts = false;
        }

        private Font GetDefaultFont()
        {
            if (messageFont != null) return messageFont;

            Debug.LogWarning("Nenhuma fonte foi assignada no 'Message Font' do DialogManager. Usando 'Arial' do sistema.");
            try
            {
                return Font.CreateDynamicFontFromOSFont("Arial", 14);
            }
            catch
            {
                Debug.LogError("FALHA AO CARREGAR FONTE! Por favor, arraste uma fonte (Arial) para o slot 'Message Font' no DialogManager.");
                return null;
            }
        }

        #endregion
    }
}
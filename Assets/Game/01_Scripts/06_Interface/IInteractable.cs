public interface IInteractable
{
    // 상호작용 시 실행될 함수
    void Interact(Player player);

    // 마우스를 올렸을 때 띄울 텍스트 (예: "줍기 [F]", "문 열기 [F]")
    string GetInteractPrompt();
}
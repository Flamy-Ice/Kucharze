#if KUCHARZE_ONLINE
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ModularMovement
{
    // Replicates a player's nickname after the server checks the owner's submission.
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class LobbyPlayerNickname : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString64Bytes> nickname = new NetworkVariable<FixedString64Bytes>();
        private bool submitted;
        public const string LoadingName = "Loading...";
        public string Nickname => nickname.Value.Length == 0 ? LoadingName : nickname.Value.ToString();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                submitted = false;
                nickname.Value = default;
            }
            string savedName = PlayerNicknameProfile.SavedName;
            if (IsOwner && PlayerNicknameProfile.IsValid(savedName))
                SubmitNicknameRpc(new FixedString64Bytes(savedName));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitNicknameRpc(FixedString64Bytes value, RpcParams rpcParams = default)
        {
            if (!IsServer || submitted || rpcParams.Receive.SenderClientId != OwnerClientId) return;
            string clean = PlayerNicknameProfile.Normalize(value.ToString());
            if (!PlayerNicknameProfile.IsValid(clean)) return;
            nickname.Value = new FixedString64Bytes(clean);
            submitted = true;
        }
    }
}
#endif

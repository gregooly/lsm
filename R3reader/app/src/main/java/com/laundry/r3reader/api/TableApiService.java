package com.laundry.r3reader.api;

import com.laundry.r3reader.api.model.BootstrapResponse;
import com.laundry.r3reader.api.model.ConnectionStatusRequest;
import com.laundry.r3reader.api.model.ConnectionStatusResponse;
import com.laundry.r3reader.api.model.LinenLookupResponse;
import com.laundry.r3reader.api.model.MovementBatchRequest;
import com.laundry.r3reader.api.model.MovementBatchResponse;
import com.laundry.r3reader.api.model.TableLoginRequest;
import com.laundry.r3reader.api.model.TableLoginResponse;

import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.GET;
import retrofit2.http.POST;
import retrofit2.http.Path;
import retrofit2.http.Query;

public interface TableApiService {

    @POST("api/auth/table-login")
    Call<TableLoginResponse> tableLogin(@Body TableLoginRequest request);

    @GET("api/table/bootstrap")
    Call<BootstrapResponse> bootstrap(@Query("readerId") int readerId);

    @GET("api/table/linen/by-tag/{rfidTag}")
    Call<LinenLookupResponse> linenByTag(
            @Path("rfidTag") String rfidTag,
            @Query("readerId") int readerId);

    @POST("api/table/linen-movement-events")
    Call<MovementBatchResponse> postMovementEvents(@Body MovementBatchRequest request);

    @POST("api/table/linen-movement-events/sync")
    Call<MovementBatchResponse> postMovementEventsSync(@Body MovementBatchRequest request);

    @POST("api/readers/table/connection-status")
    Call<ConnectionStatusResponse> postConnectionStatus(@Body ConnectionStatusRequest request);
}
